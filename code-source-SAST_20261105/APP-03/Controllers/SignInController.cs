using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tresoria.Controllers;

[ApiController]
public sealed class SignInController : ControllerBase
{
    /// <summary>
    /// Callback de retour SSO : redirige vers `returnUrl` apres authentification.
    /
    ///   relatifs, ni allowlist de domaines) : redirection ouverte vers un domaine externe.
    ///   (Code sur = n'autoriser que des chemins relatifs internes ou une allowlist.)
    /// </summary>
    [HttpGet("/signin-callback")]
    [AllowAnonymous]
    public IActionResult Callback([FromQuery] string? returnUrl)
    {
        var target = string.IsNullOrEmpty(returnUrl) ? "/dashboard" : returnUrl;
        return Redirect(target);
    }
}
