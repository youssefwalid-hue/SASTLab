using System.Security.Claims;
using Tresoria.Models;

namespace Tresoria.Services;

/// <summary>Helpers de lecture du contexte utilisateur depuis le JWT.</summary>
public static class UserContext
{
    public static string? Username(this ClaimsPrincipal u) =>
        u.FindFirst("username")?.Value ?? u.FindFirst(ClaimTypes.Name)?.Value;

    public static string? Role(this ClaimsPrincipal u) =>
        u.FindFirst("role")?.Value ?? u.FindFirst(ClaimTypes.Role)?.Value;

    public static string? Company(this ClaimsPrincipal u) =>
        u.FindFirst("company")?.Value;

    public static bool HasRole(this ClaimsPrincipal u, string role) =>
        string.Equals(u.Role(), role, StringComparison.OrdinalIgnoreCase);
}
