using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tresoria.Controllers;

[ApiController]
public sealed class HealthController : ControllerBase
{
    [HttpGet("/health")]
    [AllowAnonymous]
    public IActionResult Health() => Ok(new
    {
        status = "ok",
        app = "APP-03 Tresoria",
        environment = Environment.GetEnvironmentVariable("APP_PROFILE") ?? "local",
        lab = "test-data-fictives"
    });
}
