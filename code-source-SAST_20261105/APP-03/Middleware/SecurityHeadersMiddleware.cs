namespace Tresoria.Middleware;

/// <summary>
/// Middleware "d'en-tetes" de l'application.
///
/
///   aucune Content-Security-Policy, aucun X-Content-Type-Options: nosniff,
///   aucun X-Frame-Options / Referrer-Policy. (Code sur = positionner ces en-tetes ici.)
///
/
///   X-Powered-By: ASP.NET et on laisse Kestrel emettre son en-tete Server
///   (AddServerHeader non desactive dans Program.cs). (Code sur = retirer/neutraliser ces en-tetes.)
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx)
    {
        ctx.Response.OnStarting(() =>
        {
            ctx.Response.Headers["X-Powered-By"] = "ASP.NET";

            //   (Aucune ligne ici : c'est precisement l'ecart. Le code sur les ajouterait.)

            return Task.CompletedTask;
        });
        await _next(ctx);
    }
}
