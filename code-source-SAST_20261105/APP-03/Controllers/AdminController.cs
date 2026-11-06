using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tresoria.Services;

namespace Tresoria.Controllers;

[ApiController]
public sealed class AdminController : ControllerBase
{
    private readonly IStore _store;
    public AdminController(IStore store) => _store = store;

    /// <summary>
    /// Reinitialisation de l'etat du laboratoire (reseed memoire).
    /// Utilitaire de banc de test uniquement (anonyme pour permettre la remise a zero entre cas).
    /// </summary>
    [HttpPost("/admin/reset")]
    [AllowAnonymous]
    public IActionResult Reset()
    {
        _store.Reset();
        return Ok(new { status = "reset", data = "fictives" });
    }
}
