using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tresoria.Controllers;

/// <summary>
/// Pages de confirmation d'action sensible (approbation de lot) rendues cote serveur.
/// Ces pages portent une action a fort impact : elles ne doivent pas etre encadrables (iframe).
/// </summary>
[ApiController]
public sealed class FramingController : ControllerBase
{
    private const string ConfirmPage =
        "<!doctype html><html lang=\"fr\"><head><meta charset=\"utf-8\"><title>Confirmer l'approbation</title></head>" +
        "<body><h1>Confirmer l'approbation du lot</h1>" +
        "<p>Vous etes sur le point d'approuver un lot de virements (donnees fictives).</p>" +
        "<form method=\"post\" action=\"/api/batches/9001/approve\"><button type=\"submit\">Approuver</button></form>" +
        "</body></html>";

    /// <summary>
    /// Page de confirmation d'approbation.
    /
    ///   (ni X-Frame-Options, ni Content-Security-Policy frame-ancestors). La page d'action
    ///   sensible peut etre chargee dans une iframe tierce et detournee par superposition.
    ///   (Code sur = X-Frame-Options: DENY et/ou CSP frame-ancestors 'none' ; voir
    ///   GET /confirm-protected - N010.)
    /// </summary>
    [HttpGet("/confirm")]
    [AllowAnonymous]
    public IActionResult Confirm()
    {
        return Content(ConfirmPage, "text/html");
    }

    /// <summary>
    /// Page de confirmation FRERE, correctement protegee.
    /// NC: APP03-N010 - la meme action sensible est servie avec X-Frame-Options: DENY ET
    ///   Content-Security-Policy: frame-ancestors 'none' : tout encadrement est refuse par le
    ///   navigateur. Contraste avec GET /confirm (APP03-V019).
    /// </summary>
    [HttpGet("/confirm-protected")]
    [AllowAnonymous]
    public IActionResult ConfirmProtected()
    {
        // NC: APP03-N010 - protections anti-clickjacking explicitement positionnees.
        Response.Headers["X-Frame-Options"] = "DENY";
        Response.Headers["Content-Security-Policy"] = "frame-ancestors 'none'";
        return Content(ConfirmPage, "text/html");
    }
}
