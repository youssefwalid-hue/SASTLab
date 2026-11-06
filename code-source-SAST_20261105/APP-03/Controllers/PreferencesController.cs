using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Tresoria.Models;

namespace Tresoria.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public sealed class PreferencesController : ControllerBase
{
    /// <summary>
    /// Enregistrement des preferences utilisateur (theme, langue).
    /
    ///   avec TypeNameHandling.All, qui honore la directive "$type" du document JSON. Un attaquant
    ///   controle ainsi le type CLR instancie (vecteur classique d'execution via gadgets).
    ///   (Code sur = deserialisation vers un DTO fixe sans resolution de type ; voir
    ///   POST /api/profile/preferences-safe - N011.)
    /// </summary>
    [HttpPost("preferences")]
    public async Task<IActionResult> SavePreferences()
    {
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync();

        var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
        object? obj;
        try
        {
            obj = JsonConvert.DeserializeObject<object>(raw, settings);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "invalid_payload", detail = ex.GetType().Name });
        }

        // La preuve observable : le type reellement instancie reflete la directive attaquant.
        return Ok(new { status = "saved", deserializedType = obj?.GetType().FullName });
    }

    /// <summary>
    /// Enregistrement des preferences, variante FRERE correctement securisee.
    /// NC: APP03-N011 - le corps est lie a un DTO FIXE (PreferencesDto) via System.Text.Json,
    ///   qui N'INTERPRETE PAS "$type" : la directive de type injectee est ignoree, seuls
    ///   theme/language sont lus. Contraste avec POST /api/profile/preferences (APP03-V020).
    /// </summary>
    [HttpPost("preferences-safe")]
    public async Task<IActionResult> SavePreferencesSafe()
    {
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync();

        // NC: APP03-N011 - binding vers un type fixe, sans resolution de type ($type ignore).
        PreferencesDto? dto;
        try
        {
            dto = System.Text.Json.JsonSerializer.Deserialize<PreferencesDto>(raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (System.Text.Json.JsonException)
        {
            return BadRequest(new { error = "invalid_payload" });
        }

        return Ok(new
        {
            status = "saved",
            theme = dto?.Theme ?? "default",
            language = dto?.Language ?? "fr"
        });
    }
}
