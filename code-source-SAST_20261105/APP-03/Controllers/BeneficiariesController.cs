using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tresoria.Models;
using Tresoria.Services;

namespace Tresoria.Controllers;

[ApiController]
[Route("api/beneficiaries")]
[Authorize]
public sealed class BeneficiariesController : ControllerBase
{
    private readonly IStore _store;
    public BeneficiariesController(IStore store) => _store = store;

    /// <summary>
    /// Surface benigne : liste paginee des beneficiaires de l'entreprise du jeton.
    /// Cloisonnee par tenant ; rendu via BeneficiaryView (DTO en liste blanche, sans champ interne).
    /// </summary>
    [HttpGet]
    public IActionResult List([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;
        var company = User.Company();
        var all = _store.Beneficiaries().Where(b => b.Company == company).OrderBy(b => b.Id).ToList();
        var items = all.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(b => new BeneficiaryView { Id = b.Id, Name = b.Name, Iban = b.Iban, Verified = b.Verified });
        return Ok(new { page, pageSize, total = all.Count, items });
    }

    /// <summary>
    /// Surface benigne : export CSV des beneficiaires de l'entreprise du jeton.
    /// Cloisonne par tenant ; chaque champ est echappe (guillemets doubles) pour eviter l'injection CSV.
    /// </summary>
    [HttpGet("export.csv")]
    public IActionResult ExportCsv()
    {
        var company = User.Company();
        static string Esc(string v) => "\"" + (v ?? "").Replace("\"", "\"\"") + "\"";
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("id,name,iban,verified");
        foreach (var b in _store.Beneficiaries().Where(b => b.Company == company).OrderBy(b => b.Id))
            sb.AppendLine($"{b.Id},{Esc(b.Name)},{Esc(b.Iban)},{(b.Verified ? "true" : "false")}");
        return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "beneficiaries.csv");
    }

    /// <summary>
    /// Approbation d'un beneficiaire.
    /// NC: APP03-N001 - endpoint FRERE de l'approbation de lot, correctement securise :
    ///   verifie le role valideur ET approvedBy != createdBy ; l'approbation par le createur
    ///   est rejetee en 403. (Contraste avec POST /api/batches/{id}/approve - V001.)
    /// </summary>
    [HttpPost("{id:long}/approve")]
    public IActionResult Approve(long id)
    {
        var b = _store.GetBeneficiary(id);
        if (b is null) return NotFound(new { error = "not_found" });
        if (b.Company != User.Company())
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "forbidden" });

        // NC: APP03-N001 - role valideur requis.
        if (!User.HasRole(Roles.Validation))
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "forbidden_role" });

        // NC: APP03-N001 - separation des taches : l'approbateur ne peut pas etre le createur.
        if (string.Equals(b.CreatedBy, User.Username(), StringComparison.Ordinal))
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "four_eyes_violation" });

        b.Verified = true;
        b.ApprovedBy = User.Username();
        return Ok(new BeneficiaryView { Id = b.Id, Name = b.Name, Iban = b.Iban, Verified = b.Verified });
    }

    /// <summary>
    /// Mise a jour d'un beneficiaire.
    /// NC: APP03-N005 - entree liee a un DTO en LISTE BLANCHE (Name/Iban) : le champ `verified`
    ///   injecte est IGNORE. La sortie utilise un DTO SANS champ interne.
    ///   (Contraste avec PUT /api/batches/{id} status - V005 - et l'export - V010.)
    /// </summary>
    [HttpPut("{id:long}")]
    public IActionResult Update(long id, [FromBody] JsonElement body)
    {
        var b = _store.GetBeneficiary(id);
        if (b is null) return NotFound(new { error = "not_found" });
        if (b.Company != User.Company())
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "forbidden" });

        // NC: APP03-N005 - seuls les champs de la liste blanche sont lus ; `verified` (et tout
        //   autre champ sensible) est volontairement IGNORE, quel que soit le corps envoye.
        var dto = new UpdateBeneficiaryDto
        {
            Name = body.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : null,
            Iban = body.TryGetProperty("iban", out var i) && i.ValueKind == JsonValueKind.String ? i.GetString() : null,
        };
        if (dto.Name is not null) b.Name = dto.Name;
        if (dto.Iban is not null) b.Iban = dto.Iban;
        // `verified` n'est JAMAIS lu depuis le corps ; il n'evolue que par le workflow d'approbation.

        return Ok(new BeneficiaryView { Id = b.Id, Name = b.Name, Iban = b.Iban, Verified = b.Verified });
    }
}
