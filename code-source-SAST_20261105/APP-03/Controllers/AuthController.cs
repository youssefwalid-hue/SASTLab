using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tresoria.Models;
using Tresoria.Services;

namespace Tresoria.Controllers;

[ApiController]
[Route("api")]
public sealed class AuthController : ControllerBase
{
    private readonly IStore _store;
    private readonly TokenService _tokens;
    public AuthController(IStore store, TokenService tokens) { _store = store; _tokens = tokens; }

    /// <summary>Connexion : delivre access token (JWT) + refresh token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        var user = _store.FindUser(req.Username ?? "");
        if (user is null || user.Password != (req.Password ?? ""))
            return Unauthorized(new { error = "invalid_credentials" });

        var access = _tokens.CreateAccessToken(user);
        var refresh = _tokens.NewRandomToken();
        _store.AddRefreshToken(new RefreshTokenRecord
        {
            Token = refresh, Username = user.Username, Revoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        //   (Code sur : Secure=true + SameSite approprie.)
        Response.Headers.Append("Set-Cookie",
            $"auth={access}; Path=/; HttpOnly; SameSite=None");

        return Ok(new
        {
            access_token = access,
            refresh_token = refresh,
            token_type = "Bearer",
            expires_in = 900,
            role = user.Role,
            company = user.Company
        });
    }

    /// <summary>Deconnexion. DEVRAIT revoquer le refresh token cote serveur.</summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        //   Le code sur appellerait ici : _store.RevokeRefreshTokensForUser(User.Username()!);
        //   (volontairement omis - le refresh reste exploitable apres logout.)
        return NoContent();
    }

    /// <summary>Rafraichissement d'access token a partir d'un refresh token.</summary>
    [HttpPost("token/refresh")]
    [AllowAnonymous]
    public IActionResult Refresh([FromBody] RefreshRequest req)
    {
        var rec = _store.GetRefreshToken(req.Refresh_token ?? "");
        if (rec is null || rec.ExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { error = "invalid_refresh_token" });

        //   reste accepte. (Code sur : if (rec.Revoked) return Unauthorized();)
        var user = _store.FindUser(rec.Username);
        if (user is null) return Unauthorized(new { error = "invalid_refresh_token" });

        var access = _tokens.CreateAccessToken(user);
        return Ok(new { access_token = access, token_type = "Bearer", expires_in = 900 });
    }

    /// <summary>
    /// Reinitialisation de mot de passe par jeton a usage unique.
    /// NC: APP03-N006 - flux FRERE du refresh token, correctement securise (usage unique + expiration).
    /// </summary>
    [HttpPost("password/reset")]
    [AllowAnonymous]
    public IActionResult PasswordReset([FromBody] PasswordResetRequest req)
    {
        // NC: APP03-N006 - jeton persistant, a usage unique et expirant ; rejeu/expiration => 400.
        var rec = _store.GetResetToken(req.Reset_token ?? "");
        if (rec is null || rec.Used || rec.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { error = "invalid_or_used_reset_token" });

        if (!string.Equals(rec.Username, req.Username, StringComparison.Ordinal))
            return BadRequest(new { error = "invalid_or_used_reset_token" });

        rec.Used = true; // consommation : tout rejeu ulterieur est rejete.
        _store.SetPassword(rec.Username, req.New_password ?? Seed.LabPassword);
        return Ok(new { status = "password_reset" });
    }
}
