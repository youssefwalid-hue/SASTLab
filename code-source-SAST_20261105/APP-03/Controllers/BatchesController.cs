using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tresoria.Models;
using Tresoria.Services;

namespace Tresoria.Controllers;

[ApiController]
[Route("api/batches")]
[Authorize]
public sealed class BatchesController : ControllerBase
{
    private readonly IStore _store;
    public BatchesController(IStore store) => _store = store;

    /// <summary>Creation d'un lot (DRAFT) par le createur, dans SON entreprise.</summary>
    [HttpPost]
    public IActionResult Create([FromBody] CreateBatchRequest req)
    {
        var company = User.Company() ?? "A";
        var batch = new Batch
        {
            Id = _store.NextBatchId(company),
            Company = company,
            Label = req.Label ?? "",
            Currency = string.IsNullOrWhiteSpace(req.Currency) ? "EUR" : req.Currency!,
            Status = BatchStatus.Draft,
            CreatedBy = User.Username() ?? "",
        };
        foreach (var line in req.Transfers)
        {
            batch.Transfers.Add(new Transfer
            {
                Id = _store.NextTransferId(), BatchId = batch.Id, Company = company,
                AccountId = company == "B" ? "B-001" : "A-001",
                Iban = line.Iban ?? "", Amount = line.Amount,
                Currency = string.IsNullOrWhiteSpace(line.Currency) ? batch.Currency : line.Currency!,
                Label = line.Label ?? "", Status = BatchStatus.Draft, CreatedBy = batch.CreatedBy,
            });
        }
        _store.AddBatch(batch);
        return StatusCode(StatusCodes.Status201Created,
            new { id = batch.Id, status = batch.Status, createdBy = batch.CreatedBy });
    }

    /// <summary>Liste des lots : cloisonnee par entreprise du jeton (comportement SUR).</summary>
    [HttpGet]
    public IActionResult List()
    {
        var company = User.Company();
        var items = _store.Batches()
            .Where(b => b.Company == company)
            .Select(b => new { b.Id, b.Company, b.Label, b.Status, b.CreatedBy, count = b.Transfers.Count });
        return Ok(items);
    }

    /// <summary>
    /// Consultation d'un lot par identifiant.
    /
    /
    ///   exception non geree, et la Developer Exception Page (active en prod) renvoie la stack trace.
    /// </summary>
    [HttpGet("{id}")]
    public IActionResult Get(string id)
    {
        long batchId = long.Parse(id);

        var batch = _store.GetBatch(batchId);
        if (batch is null) return NotFound(new { error = "not_found" });

        return Ok(new
        {
            id = batch.Id, company = batch.Company, label = batch.Label,
            status = batch.Status, createdBy = batch.CreatedBy,
            approvedBy = batch.ApprovedBy, validatedBy = batch.ValidatedBy,
            transfers = batch.Transfers.Select(t => new
            {
                t.Id, t.Iban, t.Amount, t.Currency, t.Label, t.Status // DTO fonctionnel (pas de champ interne ici)
            })
        });
    }

    /// <summary>
    /// Mise a jour d'un lot.
    /
    ///   ce qui permet de forcer un statut hors workflow. (Code sur = UpdateBatchDto en liste blanche.)
    /// </summary>
    [HttpPut("{id:long}")]
    public IActionResult Update(long id, [FromBody] JsonElement body)
    {
        var batch = _store.GetBatch(id);
        if (batch is null) return NotFound(new { error = "not_found" });
        if (batch.Company != User.Company()) return Forbid();

        // Champs fonctionnels (toujours autorises).
        if (body.TryGetProperty("label", out var label) && label.ValueKind == JsonValueKind.String)
            batch.Label = label.GetString() ?? batch.Label;
        if (body.TryGetProperty("currency", out var cur) && cur.ValueKind == JsonValueKind.String)
            batch.Currency = cur.GetString() ?? batch.Currency;

        //   Le code sur lierait UpdateBatchDto (sans `status`) et ignorerait ce champ.
        if (body.TryGetProperty("status", out var st) && st.ValueKind == JsonValueKind.String)
            batch.Status = st.GetString() ?? batch.Status;

        return Ok(new { id = batch.Id, status = batch.Status, label = batch.Label });
    }

    /// <summary>Soumission d'un lot DRAFT -> PENDING_APPROVAL.</summary>
    [HttpPost("{id:long}/submit")]
    public IActionResult Submit(long id)
    {
        var batch = _store.GetBatch(id);
        if (batch is null) return NotFound(new { error = "not_found" });
        if (batch.Company != User.Company()) return Forbid();
        if (batch.Status != BatchStatus.Draft)
            return Conflict(new { error = "invalid_state", status = batch.Status });

        batch.Status = BatchStatus.PendingApproval;
        batch.SubmittedBy = User.Username();
        foreach (var t in batch.Transfers) t.Status = BatchStatus.PendingApproval;
        return Ok(new { status = batch.Status });
    }

    /// <summary>
    /// Validation d'un lot (DEVRAIT etre reservee au role valideur).
    /
    ///   peut valider. (Code sur = exiger le role validation, sinon 403.)
    /// </summary>
    [HttpPost("{id:long}/validate")]
    public IActionResult Validate(long id)
    {
        var batch = _store.GetBatch(id);
        if (batch is null) return NotFound(new { error = "not_found" });
        if (batch.Company != User.Company()) return Forbid();

        batch.Status = BatchStatus.Validated;
        batch.ValidatedBy = User.Username();
        return Ok(new { status = batch.Status, validatedBy = batch.ValidatedBy });
    }

    /// <summary>
    /// Approbation d'un lot (regle des quatre yeux).
    /
    ///   createur != approbateur : le createur peut approuver son propre lot.
    ///   (Code sur = exiger role validation ET approvedBy != createdBy, sinon 403/409.)
    /// </summary>
    [HttpPost("{id:long}/approve")]
    public IActionResult Approve(long id)
    {
        var batch = _store.GetBatch(id);
        if (batch is null) return NotFound(new { error = "not_found" });
        if (batch.Company != User.Company()) return Forbid();
        if (batch.Status is not (BatchStatus.PendingApproval or BatchStatus.Validated))
            return Conflict(new { error = "invalid_state", status = batch.Status });

        //   Le code sur ferait :
        //     if (!User.HasRole(Roles.Validation)) return Forbid();
        //     if (batch.CreatedBy == User.Username()) return Conflict("four_eyes_violation");
        batch.Status = BatchStatus.Approved;
        batch.ApprovedBy = User.Username();
        return Ok(new { status = batch.Status, approvedBy = batch.ApprovedBy, createdBy = batch.CreatedBy });
    }

    /// <summary>
    /// Export d'un lot.
    /
    ///   (internalCost, fraudScore, internalNote). (Code sur = DTO de sortie en liste blanche.)
    /// </summary>
    [HttpGet("{id:long}/export")]
    public IActionResult Export(long id)
    {
        var batch = _store.GetBatch(id);
        if (batch is null) return NotFound(new { error = "not_found" });
        if (batch.Company != User.Company()) return Forbid();

        return Ok(new
        {
            id = batch.Id, company = batch.Company, label = batch.Label, status = batch.Status,
            internalCost = batch.InternalCost, fraudScore = batch.FraudScore, internalNote = batch.InternalNote,
            transfers = batch.Transfers.Select(t => new
            {
                t.Iban, t.Amount, t.Currency, t.Label,
                internalCost = t.InternalCost, fraudScore = t.FraudScore, internalNote = t.InternalNote
            })
        });
    }

    /// <summary>
    /// Suppression d'un lot.
    /// NC: APP03-N002 - fonction d'administration FRERE, correctement RESERVEE au role valideur :
    ///   un jeton sans ce role recoit 403 (controle fonctionnel cote serveur, independamment de l'UI).
    /// </summary>
    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id)
    {
        // NC: APP03-N002 - autorisation fonctionnelle verifiee cote serveur.
        if (!User.HasRole(Roles.Validation) && !User.HasRole(Roles.Admin))
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "forbidden" });

        var batch = _store.GetBatch(id);
        if (batch is null) return NotFound(new { error = "not_found" });
        if (batch.Company != User.Company())
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "forbidden" });

        _store.RemoveBatch(id);
        return NoContent();
    }
}
