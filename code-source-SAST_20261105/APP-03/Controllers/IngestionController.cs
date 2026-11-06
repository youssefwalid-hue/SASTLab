using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tresoria.Models;

namespace Tresoria.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public sealed class IngestionController : ControllerBase
{
    // Allowlist (NC N013) des hotes de rapports externes legitimes.
    private static readonly HashSet<string> HostAllowlist =
        new(StringComparer.OrdinalIgnoreCase) { "reports.tresoria.lab.exemple" };

    private static async Task<(int status, int length, string snippet)> FetchAsync(string url)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        var resp = await http.GetAsync(url);
        var body = await resp.Content.ReadAsStringAsync();
        var snippet = body.Length > 256 ? body.Substring(0, 256) : body;
        return ((int)resp.StatusCode, body.Length, snippet);
    }

    /// <summary>
    /// Import d'un rapport de tresorerie depuis une URL fournie par l'appelant.
    /
    ///   pas de blocage des adresses internes/loopback/metadonnees). Un attaquant force le serveur
    ///   a emettre des requetes vers des ressources internes et recoit leur contenu.
    ///   (Charges de laboratoire ciblant EXCLUSIVEMENT l'hote du lab.) Le code sur validerait l'URL
    ///   contre une allowlist et bloquerait les adresses internes ; voir POST /api/reports/import-safe - N013.
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] ImportReportRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Url))
            return BadRequest(new { error = "missing_url" });

        try
        {
            var (status, length, snippet) = await FetchAsync(req.Url);
            return Ok(new { fetched = true, url = req.Url, status, length, snippet });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "fetch_failed", detail = ex.GetType().Name });
        }
    }

    /// <summary>
    /// Import de rapport FRERE, correctement valide.
    /// NC: APP03-N013 - l'URL est validee contre une allowlist d'hotes externes ; les schemes
    ///   non-HTTP(S), les hotes hors allowlist et les adresses internes/loopback sont rejetes en 400.
    ///   Contraste avec POST /api/reports/import (APP03-V022).
    /// </summary>
    [HttpPost("import-safe")]
    public async Task<IActionResult> ImportSafe([FromBody] ImportReportRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Url) ||
            !Uri.TryCreate(req.Url, UriKind.Absolute, out var uri))
            return BadRequest(new { error = "invalid_url" });

        // NC: APP03-N013 - schemes autorises + allowlist stricte d'hotes externes.
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            return BadRequest(new { error = "scheme_not_allowed" });
        if (uri.IsLoopback || !HostAllowlist.Contains(uri.Host))
            return BadRequest(new { error = "host_not_allowed" });

        try
        {
            var (status, length, snippet) = await FetchAsync(uri.ToString());
            return Ok(new { fetched = true, url = uri.ToString(), status, length, snippet });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "fetch_failed", detail = ex.GetType().Name });
        }
    }
}
