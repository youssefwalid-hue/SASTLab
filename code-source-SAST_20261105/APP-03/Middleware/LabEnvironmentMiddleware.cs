namespace Tresoria.Middleware;

/// <summary>
/// Ajoute l'en-tete d'identification du laboratoire a TOUTES les reponses.
/// Marque explicitement l'environnement comme contenant des donnees fictives.
/// </summary>
public sealed class LabEnvironmentMiddleware
{
    private readonly RequestDelegate _next;
    public LabEnvironmentMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx)
    {
        ctx.Response.OnStarting(() =>
        {
            ctx.Response.Headers["X-Lab-Environment"] = "test-data-fictives";
            return Task.CompletedTask;
        });
        await _next(ctx);
    }
}
