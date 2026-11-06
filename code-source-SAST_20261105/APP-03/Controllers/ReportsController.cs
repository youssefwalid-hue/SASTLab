using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tresoria.Services;

namespace Tresoria.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public sealed class ReportsController : ControllerBase
{
    private readonly ReportService _reports;
    public ReportsController(ReportService reports) => _reports = reports;

    /// <summary>
    /// Reporting de l'entreprise du jeton.
    /
    /// NC: APP03-N007 - `groupBy` est, lui, mappe en liste blanche (pas de concatenation SQL),
    ///   et les valeurs restituees sont encodees cote SPA (pas de [innerHTML]).
    /// </summary>
    [HttpGet]
    public IActionResult Get(
        [FromQuery] string? sort,
        [FromQuery] string? metric,
        [FromQuery] string? accountId,
        [FromQuery] string? groupBy)
    {
        var company = User.Company() ?? "A";
        try
        {
            var result = _reports.Build(company, sort, metric, accountId, groupBy);
            return Ok(result);
        }
        catch (ArgumentException)
        {
            // NC: APP03-N007 - valeur de groupBy hors liste blanche => 400 (aucune concatenation SQL).
            return BadRequest(new { error = "invalid_groupBy" });
        }
    }
}
