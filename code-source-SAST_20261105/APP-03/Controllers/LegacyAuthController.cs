using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Tresoria.Services;

namespace Tresoria.Controllers;

/// <summary>
/// Endpoint "legacy" d'introspection de jeton (compatibilite d'un ancien portail).
/// Il accepte un Bearer et renvoie l'identite portee par le jeton.
/// </summary>
[ApiController]
[Route("api/legacy")]
public sealed class LegacyAuthController : ControllerBase
{
    private readonly TokenService _tokens;
    public LegacyAuthController(TokenService tokens) => _tokens = tokens;

    private static string? BearerToken(HttpRequest req)
    {
        var h = req.Headers["Authorization"].ToString();
        return h.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? h.Substring(7).Trim() : null;
    }

    /// <summary>
    /// Introspection "legacy" du jeton Bearer.
    /
    ///   ValidateIssuerSigningKey=false et un SignatureValidator qui renvoie le jeton tel quel.
    ///   Un jeton forge (signature arbitraire ou alg none) portant role=admin est donc accepte
    ///   et son identite est fait confiance. (Code sur = valider la signature avec la cle de
    ///   l'IdP ; voir GET /api/legacy/whoami-strict - N012.)
    /// </summary>
    [HttpGet("whoami")]
    [AllowAnonymous]
    public IActionResult WhoAmI()
    {
        var raw = BearerToken(Request);
        if (string.IsNullOrEmpty(raw)) return Unauthorized(new { error = "missing_token" });

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var parms = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = false,
            RequireSignedTokens = false,
            SignatureValidator = (token, _) => new JwtSecurityToken(token),
        };
        try
        {
            var principal = handler.ValidateToken(raw, parms, out _);
            return Ok(new
            {
                authenticated = true,
                username = principal.FindFirst("username")?.Value,
                role = principal.FindFirst("role")?.Value,
                company = principal.FindFirst("company")?.Value,
            });
        }
        catch (Exception ex)
        {
            return Unauthorized(new { error = "invalid_token", detail = ex.GetType().Name });
        }
    }

    /// <summary>
    /// Introspection FRERE, correctement securisee.
    /// NC: APP03-N012 - la signature EST verifiee avec la cle de l'IdP (ValidateIssuerSigningKey=true,
    ///   RequireSignedTokens=true) ; un jeton forge ou mal signe est rejete en 401. Contraste avec
    ///   GET /api/legacy/whoami (APP03-V021).
    /// </summary>
    [HttpGet("whoami-strict")]
    [AllowAnonymous]
    public IActionResult WhoAmIStrict()
    {
        var raw = BearerToken(Request);
        if (string.IsNullOrEmpty(raw)) return Unauthorized(new { error = "missing_token" });

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var parms = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            ValidIssuer = TokenService.Issuer,
            ValidAudience = TokenService.Audience,
            // NC: APP03-N012 - la signature est verifiee contre la cle de l'IdP.
            IssuerSigningKey = _tokens.SigningKey,
        };
        try
        {
            var principal = handler.ValidateToken(raw, parms, out _);
            return Ok(new
            {
                authenticated = true,
                username = principal.FindFirst("username")?.Value,
                role = principal.FindFirst("role")?.Value,
                company = principal.FindFirst("company")?.Value,
            });
        }
        catch (Exception ex)
        {
            return Unauthorized(new { error = "invalid_token", detail = ex.GetType().Name });
        }
    }
}
