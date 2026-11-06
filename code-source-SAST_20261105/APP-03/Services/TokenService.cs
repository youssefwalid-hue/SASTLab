using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Tresoria.Models;

namespace Tresoria.Services;

/// <summary>
/// Emission JWT (access token court) + refresh tokens. Cle symetrique de LABORATOIRE
/// (fictive, non secrete). En prod, cle via configuration/secret manager.
/// </summary>
public sealed class TokenService
{
    public const string Issuer = "https://tresoria-idp.lab.exemple";
    public const string Audience = "tresoria-api";
    // Cle de laboratoire 100% fictive (jamais un secret reel) - >= 32 octets.
    private const string LabKey = "tresoria-lab-signing-key-fictive-0123456789ABCDEF";

    public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(LabKey));

    public string CreateAccessToken(User user)
    {
        var creds = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim("username", user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("role", user.Role),
            new Claim("company", user.Company),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };
        var token = new JwtSecurityToken(
            issuer: Issuer, audience: Audience, claims: claims,
            notBefore: DateTime.UtcNow, expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string NewRandomToken(int bytes = 32)
    {
        var buf = RandomNumberGenerator.GetBytes(bytes);
        return Convert.ToHexString(buf).ToLowerInvariant();
    }
}
