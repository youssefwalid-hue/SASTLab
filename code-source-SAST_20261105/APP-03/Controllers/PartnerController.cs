using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tresoria.Services;

namespace Tresoria.Controllers;

[ApiController]
[Route("api/partner")]
[Authorize]
public sealed class PartnerController : ControllerBase
{
    private readonly IStore _store;
    public PartnerController(IStore store) => _store = store;

    // Allowlist (NC N008) : seuls ces domaines partenaires sont des origines cross-site legitimes.
    private static readonly HashSet<string> OriginAllowlist =
        new(StringComparer.OrdinalIgnoreCase) { "https://partners.tresoria.lab.exemple" };

    /// <summary>
    /// Profil partenaire expose en cross-origin (API partenaires).
    /
    ///   Access-Control-Allow-Origin, avec Access-Control-Allow-Credentials: true.
    ///   N'importe quelle origine (y compris malveillante) est donc autorisee a lire la
    ///   reponse authentifiee. (Code sur = ne refleter qu'une allowlist d'origines, sans
    ///   credentials pour une origine arbitraire ; voir GET /api/partner/public - N008.)
    /// </summary>
    [HttpGet("profile")]
    public IActionResult Profile()
    {
        var origin = Request.Headers["Origin"].ToString();
        if (!string.IsNullOrEmpty(origin))
        {
            Response.Headers["Access-Control-Allow-Origin"] = origin;
            Response.Headers["Access-Control-Allow-Credentials"] = "true";
            Response.Headers["Vary"] = "Origin";
        }
        var user = _store.FindUser(User.Username() ?? "");
        return Ok(new
        {
            username = user?.Username,
            company = user?.Company,
            role = user?.Role,
            partnerScope = "treasury.read"
        });
    }

    /// <summary>
    /// Donnees partenaires publiques exposees en cross-origin.
    /// NC: APP03-N008 - endpoint FRERE correctement configure : l'Origin n'est reflete QUE
    ///   s'il appartient a une allowlist stricte ; une origine arbitraire n'obtient pas
    ///   Access-Control-Allow-Origin (et jamais de credentials reflechis). Contraste avec
    ///   GET /api/partner/profile (APP03-V017).
    /// </summary>
    [HttpGet("public")]
    public IActionResult Public()
    {
        var origin = Request.Headers["Origin"].ToString();
        // NC: APP03-N008 - uniquement les origines de l'allowlist sont autorisees.
        if (!string.IsNullOrEmpty(origin) && OriginAllowlist.Contains(origin))
        {
            Response.Headers["Access-Control-Allow-Origin"] = origin;
            Response.Headers["Vary"] = "Origin";
            // Pas de credentials reflechis pour une ressource partagee.
        }
        var company = User.Company() ?? "A";
        return Ok(new { company, catalog = "treasury-public", items = 0 });
    }
}
