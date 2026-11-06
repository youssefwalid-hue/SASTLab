using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tresoria.Models;
using Tresoria.Services;

namespace Tresoria.Controllers;

[ApiController]
[Route("api/password")]
public sealed class AccountRecoveryController : ControllerBase
{
    private readonly IStore _store;
    public AccountRecoveryController(IStore store) => _store = store;

    /// <summary>
    /// Demande de lien de reinitialisation de mot de passe.
    /
    ///   (200 {status:reset_sent}) d'un compte inconnu (404 {error:unknown_user}). Un attaquant
    ///   peut ainsi valider l'existence d'identifiants. (Code sur = reponse UNIFORME quel que soit
    ///   le compte ; voir POST /api/password/forgot-uniform - N009.)
    /// </summary>
    [HttpPost("forgot")]
    [AllowAnonymous]
    public IActionResult Forgot([FromBody] ForgotPasswordRequest req)
    {
        var user = _store.FindUser(req.Username ?? "");
        if (user is null)
            return NotFound(new { error = "unknown_user" });

        return Ok(new { status = "reset_sent", username = user.Username });
    }

    /// <summary>
    /// Demande de lien de reinitialisation, variante FRERE correctement concue.
    /// NC: APP03-N009 - reponse UNIFORME (202, meme corps) que le compte existe ou non ;
    ///   aucune divulgation d'existence. Contraste avec POST /api/password/forgot (APP03-V018).
    /// </summary>
    [HttpPost("forgot-uniform")]
    [AllowAnonymous]
    public IActionResult ForgotUniform([FromBody] ForgotPasswordRequest req)
    {
        // NC: APP03-N009 - traitement identique ; aucune difference observable entre compte connu/inconnu.
        _ = _store.FindUser(req.Username ?? "");  // un email n'est reellement envoye que si le compte existe
        return Accepted(new { status = "if_account_exists_email_sent" });
    }
}
