using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tresoria.Services;

namespace Tresoria.Controllers;

/// <summary>
/// Surfaces fonctionnelles BENIGNES (profil, notifications, aide, historique) : routes sures,
/// authentifiees, cloisonnees par entreprise et a sortie echappee. Aucune n'introduit d'ecart ;
/// elles elargissent la surface de crawl du laboratoire.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public sealed class SelfServiceController : ControllerBase
{
    private readonly IStore _store;
    public SelfServiceController(IStore store) => _store = store;

    /// <summary>Profil de l'utilisateur courant (lu depuis le jeton, aucune donnee d'un tiers).</summary>
    [HttpGet("profile")]
    public IActionResult Profile()
    {
        var user = _store.FindUser(User.Username() ?? "");
        if (user is null) return NotFound(new { error = "not_found" });
        return Ok(new { username = user.Username, role = user.Role, company = user.Company, email = user.Email, phone = user.Phone });
    }

    /// <summary>Notifications statiques de l'utilisateur (fictives).</summary>
    [HttpGet("notifications")]
    public IActionResult Notifications()
    {
        var company = User.Company() ?? "A";
        return Ok(new
        {
            company,
            items = new[]
            {
                new { id = 1, kind = "info",    message = "Bienvenue sur Tresoria (environnement de laboratoire)." },
                new { id = 2, kind = "reminder", message = "Pensez a soumettre vos lots avant la cloture quotidienne." },
            }
        });
    }

    /// <summary>Historique pagine des virements de l'entreprise du jeton (cloisonne).</summary>
    [HttpGet("transfers/history")]
    public IActionResult History([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;
        var company = User.Company();
        var all = _store.Transfers().Where(t => t.Company == company).OrderBy(t => t.Id).ToList();
        var items = all.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new { t.Id, t.BatchId, t.Iban, t.Amount, t.Currency, t.Label, t.Status, t.CreatedAt });
        return Ok(new { page, pageSize, total = all.Count, items });
    }

    /// <summary>Page d'aide / a-propos (contenu statique, donnees fictives).</summary>
    [HttpGet("about")]
    [AllowAnonymous]
    public IActionResult About() => Ok(new
    {
        app = "Tresoria",
        description = "Portail de tresorerie entreprises - environnement de laboratoire DAST.",
        data = "100% fictives",
        support = "support@tresoria.lab.exemple"
    });
}
