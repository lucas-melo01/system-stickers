using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.API.Endpoints;
using SistemaEtiquetas.API.Extensions;
using SistemaEtiquetas.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(databaseUrl));

builder.Services.AddSistemaEtiquetasCors(builder.Configuration);
builder.Services.AddSistemaEtiquetasAuth(builder.Configuration);

builder.Services.AddScoped<SistemaEtiquetas.API.Services.ImpressaoService>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

var app = builder.Build();

app.UseCors("Web");

var authConfigured = ApiV1Routes.IsAuthConfigured(app.Configuration);
if (authConfigured)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch
    {
    }
}

app.MapApiV1(app.Configuration);

var reimprimir = app.MapPost("/reimprimir/{itemId:int}", LegacyReimprimir.RetornarZpl);
if (authConfigured)
    reimprimir.RequireAuthorization();

app.MapPost("/webhook/pedido/resumemodas", async (HttpContext http, AppDbContext db, IConfiguration cfg, ILoggerFactory lf) =>
{
    if (!WebhookSecretHelper.Validate(cfg, http.Request.Headers))
        return Results.Unauthorized();
    var payload = await new StreamReader(http.Request.Body).ReadToEndAsync();
    return await WebhookPedidoHandler.Processar(payload, "Resume Modas", db, lf.CreateLogger("webhook"));
});

app.MapPost("/webhook/pedido/donnakora", async (HttpContext http, AppDbContext db, IConfiguration cfg, ILoggerFactory lf) =>
{
    if (!WebhookSecretHelper.Validate(cfg, http.Request.Headers))
        return Results.Unauthorized();
    var payload = await new StreamReader(http.Request.Body).ReadToEndAsync();
    return await WebhookPedidoHandler.Processar(payload, "DonnaKora", db, lf.CreateLogger("webhook"));
});

app.MapGet("/", () => "API Sistema Etiquetas");

app.MapGet("/diag/cors", () => Results.Ok(new
{
    origins = SistemaEtiquetas.API.Extensions.ServiceCollectionExtensions.CorsOrigens,
    corsOriginsEnv = Environment.GetEnvironmentVariable("CORS_ORIGINS"),
}));

app.MapGet("/health", (AppDbContext db) =>
{
    try
    {
        return db.Database.CanConnect()
            ? Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow })
            : Results.StatusCode(503);
    }
    catch
    {
        return Results.StatusCode(503);
    }
});

app.Run();
