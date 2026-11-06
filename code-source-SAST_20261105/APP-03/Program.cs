// =============================================================================
// APP-03 "Tresoria" - Portail de tresorerie entreprises (DAST Benchmark Lab).
// Laboratoire DEFENSIF - DONNEES 100% FICTIVES - Securise PAR DEFAUT.
// Seules les 22 vulnerabilites APP03-V001..V022 sont presentes (ecarts localises).
// Profils : local (memoire, port 8080) / prod (SQL Server + Redis, port 5000) /
//           standalone (memoire, port 5000, deploiement EB sans base).
// =============================================================================
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Tresoria.Middleware;
using Tresoria.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Profil & port -----------------------------------------------------------
var profile = Environment.GetEnvironmentVariable("APP_PROFILE")
              ?? builder.Configuration["AppProfile"] ?? "local";
int port = profile.Equals("local", StringComparison.OrdinalIgnoreCase) ? 8080 : 5000;
if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var envPort) && envPort > 0)
    port = envPort;
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = true);

// --- Services ----------------------------------------------------------------
// Stockage : EN MEMOIRE pour local/standalone. En profil `prod`, une implementation
// SQL Server (+ Redis via DB_URL/REDIS_URL) remplacerait MemoryStore derriere IStore.
builder.Services.AddSingleton<IStore, MemoryStore>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<ReportService>();

builder.Services.AddControllers();

var tokenService = new TokenService();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = TokenService.Issuer,
            ValidAudience = TokenService.Audience,
            IssuerSigningKey = tokenService.SigningKey,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Tresoria API (DAST Benchmark Lab - donnees fictives)",
        Version = "v1",
        Description = "Portail de tresorerie entreprises - laboratoire defensif."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference
            { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
    });
});

var app = builder.Build();

// --- Pipeline ----------------------------------------------------------------
// 1) En-tete d'identification du laboratoire (toutes reponses).
app.UseMiddleware<LabEnvironmentMiddleware>();

// 2) En-tetes "securite" : GT: APP03-V014 (durcissement absent) + GT: APP03-V016 (X-Powered-By).
app.UseMiddleware<SecurityHeadersMiddleware>();

// 3) GT: APP03-V013 - Developer Exception Page ACTIVE (meme hors developpement) :
//    une exception non geree renvoie la stack trace .NET detaillee.
app.UseDeveloperExceptionPage();

// 4) GT: APP03-V015 - filtrage de chemin "proxy" contournable par double slash.
app.UseMiddleware<SwaggerPathMiddleware>();

// 5) Swagger / OpenAPI sur /swagger.
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tresoria API v1"));

// 6) SPA statique (rendu des libelles - V009 - et navigation).
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Repli SPA : les routes non-API renvoient l'index (sauf swagger/health/admin/api/signin-callback).
app.MapFallbackToFile("index.html");

app.Run();
