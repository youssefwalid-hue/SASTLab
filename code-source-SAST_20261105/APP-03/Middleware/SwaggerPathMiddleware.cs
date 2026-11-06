namespace Tresoria.Middleware;

/// <summary>
/// Simulation du filtrage de chemin d'un reverse proxy devant Swagger.
///
/
/// mais le backend NORMALISE un double slash : /swagger//v1/swagger.json est reecrit vers
/// la forme canonique et sert alors le document OpenAPI. Le filtre, sensible a la forme
/// litterale du chemin, est donc contourne.
///
/// Code sur : normaliser le chemin AVANT filtrage (ou desactiver Swagger en production).
/// </summary>
public sealed class SwaggerPathMiddleware
{
    private readonly RequestDelegate _next;
    public SwaggerPathMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx)
    {
        var raw = ctx.Request.Path.Value ?? "";

        // Regle de blocage "proxy" sur la forme litterale canonique.
        if (raw.Equals("/swagger/v1/swagger.json", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            await ctx.Response.WriteAsync("Blocked by proxy policy");
            return;
        }

        if (raw.Contains("/swagger//", StringComparison.OrdinalIgnoreCase))
        {
            var normalized = raw.Replace("/swagger//", "/swagger/");
            ctx.Request.Path = normalized; // normalisation backend -> Swagger sert le document
        }

        await _next(ctx);
    }
}
