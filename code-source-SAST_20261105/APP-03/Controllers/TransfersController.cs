using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tresoria.Models;
using Tresoria.Services;

namespace Tresoria.Controllers;

[ApiController]
[Route("api/transfers")]
[Authorize]
public sealed class TransfersController : ControllerBase
{
    private readonly IStore _store;
    public TransfersController(IStore store) => _store = store;

    // Liste blanche ISO 4217 (NC N003) pour le parametre FRERE `currency`.
    private static readonly HashSet<string> CurrencyWhitelist =
        new(StringComparer.OrdinalIgnoreCase) { "EUR", "USD", "GBP", "CHF", "JPY" };

    /// <summary>
    /// Creation d'un virement.
    /
    ///   est accepte et inverse le sens de l'operation.
    /
    ///   lecture du cumul, fenetre, puis insertion -> plusieurs requetes concurrentes passent.
    /
    /// NC: APP03-N003 - le parametre FRERE `currency` est, lui, valide en liste blanche (400 sinon).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransferRequest req)
    {
        var company = User.Company() ?? "A";
        var accountId = string.IsNullOrWhiteSpace(req.AccountId)
            ? (company == "B" ? "B-001" : "A-001") : req.AccountId;

        var account = _store.FindAccount(accountId);
        if (account is null || account.Company != company)
            return BadRequest(new { error = "invalid_account" });

        // NC: APP03-N003 - validation de `currency` en liste blanche ISO 4217.
        var currency = string.IsNullOrWhiteSpace(req.Currency) ? "EUR" : req.Currency!;
        if (!CurrencyWhitelist.Contains(currency))
            return BadRequest(new { error = "invalid_currency" });

        //   Le code sur ferait : if (req.Amount <= 0 || req.Amount > MAX) return BadRequest();

        // --- Controle du plafond quotidien, VOLONTAIREMENT non atomique. ---
        var cap = _store.FindCompany(company)?.DailyCap ?? 10000.00m;
        var current = _store.DailyTotal(accountId);        // (1) lecture du cumul
        if (req.Amount > 0 && current + req.Amount > cap)
            return BadRequest(new { error = "daily_cap_exceeded", cap, current });

        await Task.Delay(40);                               // (2) fenetre de course

        var transfer = new Transfer
        {
            Id = _store.NextTransferId(),
            BatchId = req.BatchId ?? (company == "B" ? 9005 : 9001),
            Company = company, AccountId = accountId,
            Iban = req.Iban ?? "", Amount = req.Amount, Currency = currency,
            Label = req.Label ?? "",
            Status = BatchStatus.PendingApproval, CreatedBy = User.Username() ?? "",
        };
        _store.AddTransfer(transfer);                        // (3) insertion

        return StatusCode(StatusCodes.Status201Created, new
        {
            id = transfer.Id, batchId = transfer.BatchId, amount = transfer.Amount,
            currency = transfer.Currency, label = transfer.Label, status = transfer.Status
        });
    }

    /// <summary>
    /// Liste des virements d'un lot (cloisonnee par entreprise).
    /
    /// </summary>
    [HttpGet]
    public IActionResult ListByBatch([FromQuery] long batchId)
    {
        var company = User.Company();
        var items = _store.Transfers()
            .Where(t => t.BatchId == batchId && t.Company == company)
            .Select(t => new { t.Id, t.Iban, t.Amount, t.Currency, t.Label, t.Status });
        return Ok(items);
    }

    /// <summary>
    /// Consultation d'un virement par identifiant.
    /// NC: APP03-N004 - ressource FRERE correctement CLOISONNEE : toute lecture est filtree par
    ///   l'entreprise du jeton ; un identifiant hors tenant renvoie 404.
    /// </summary>
    [HttpGet("{id:long}")]
    public IActionResult GetById(long id)
    {
        var transfer = _store.GetTransfer(id);
        // NC: APP03-N004 - filtre par tenant (contrairement a GET /api/batches/{id} - V004).
        if (transfer is null || transfer.Company != User.Company())
            return NotFound(new { error = "not_found" });

        return Ok(new { transfer.Id, transfer.Iban, transfer.Amount, transfer.Currency, transfer.Label, transfer.Status });
    }
}
