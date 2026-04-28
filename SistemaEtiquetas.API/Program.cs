using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.API.Endpoints;
using SistemaEtiquetas.API.Extensions;
using SistemaEtiquetas.API.Services;
using SistemaEtiquetas.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(databaseUrl));

builder.Services.AddSistemaEtiquetasCors(builder.Configuration);
builder.Services.AddSistemaEtiquetasAuth(builder.Configuration);

builder.Services.AddScoped<SistemaEtiquetas.API.Services.ImpressaoService>();

builder.Services.Configure<LojaIntegradaApiOptions>(
    builder.Configuration.GetSection("LojaIntegrada"));
builder.Services.AddHttpClient<LojaIntegradaProdutoApi>();

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

app.MapPost("/webhook/pedido/resumemodas", async (HttpContext http, AppDbContext db, IConfiguration cfg, LojaIntegradaProdutoApi catalogo) =>
{
    if (!WebhookSecretHelper.Validate(cfg, http.Request.Headers))
        return Results.Unauthorized();
    var payload = await new StreamReader(http.Request.Body).ReadToEndAsync();
    return await WebhookPedidoHandler.Processar(payload, "Resume Modas", db, catalogo);
});

app.MapPost("/webhook/pedido/donnakora", async (HttpContext http, AppDbContext db, IConfiguration cfg, LojaIntegradaProdutoApi catalogo) =>
{
    if (!WebhookSecretHelper.Validate(cfg, http.Request.Headers))
        return Results.Unauthorized();
    var payload = await new StreamReader(http.Request.Body).ReadToEndAsync();
    return await WebhookPedidoHandler.Processar(payload, "DonnaKora", db, catalogo);
});

app.MapGet("/", () => "API Sistema Etiquetas");

app.MapGet("/diag/cors", () => Results.Ok(new
{
    origins = SistemaEtiquetas.API.Extensions.ServiceCollectionExtensions.CorsOrigens,
    corsOriginsEnv = Environment.GetEnvironmentVariable("CORS_ORIGINS"),
}));

app.MapGet("/diag/auth", (IConfiguration cfg) =>
{
    var diag = SistemaEtiquetas.API.Extensions.ServiceCollectionExtensions.AuthDiag;
    return Results.Ok(new
    {
        authConfigured = ApiV1Routes.IsAuthConfigured(cfg),
        algoritmoEsperado = diag.Algoritmo,
        issuerEsperado = diag.IssuerEsperado,
        audienceEsperada = diag.AudienceEsperada,
        supabaseUrl = diag.SupabaseUrl,
        secretTamanho = diag.SecretTamanho,
        secretUltimos4 = diag.SecretUltimos4,
        bootstrapAdminEmails = cfg["Auth:BootstrapAdminEmails"] ?? Environment.GetEnvironmentVariable("BOOTSTRAP_ADMIN_EMAILS"),
    });
});

// Inspecciona um JWT enviado no header Authorization e, sem expor o segredo,
// devolve alg/iss/aud/exp/sub + o motivo exacto da falha de validação.
// Serve para confirmar se o token recebido é HS256 e bate com o que a API tem.
app.MapGet("/diag/token", (HttpContext http, IConfiguration cfg) =>
{
    var authHeader = http.Request.Headers["Authorization"].ToString();
    if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "Envie Authorization: Bearer <access_token>" });

    var token = authHeader["Bearer ".Length..].Trim();
    var diag = SistemaEtiquetas.API.Extensions.ServiceCollectionExtensions.AuthDiag;
    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();

    System.IdentityModel.Tokens.Jwt.JwtSecurityToken? parsed;
    try
    {
        parsed = handler.ReadJwtToken(token);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = "Token não é JWT válido", detalhe = ex.Message });
    }

    string? validacao = null;
    if (diag.Configurado)
    {
        var secretRaw = cfg["Auth:SupabaseJwtSecret"] ?? Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET") ?? "";
        var secret = secretRaw.Trim().Trim('"', '\'');
        var parms = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret)),
            ValidAlgorithms = new[] { Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256 },
            ValidIssuer = diag.IssuerEsperado,
            ValidAudience = diag.AudienceEsperada,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
        try
        {
            handler.ValidateToken(token, parms, out _);
            validacao = "OK";
        }
        catch (Exception ex)
        {
            validacao = $"{ex.GetType().Name}: {ex.Message}";
        }
    }
    else
    {
        validacao = "Auth não configurada na API";
    }

    return Results.Ok(new
    {
        alg = parsed.Header.Alg,
        typ = parsed.Header.Typ,
        iss = parsed.Issuer,
        aud = parsed.Audiences,
        sub = parsed.Subject,
        exp = parsed.ValidTo,
        nbf = parsed.ValidFrom,
        email = parsed.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
        role = parsed.Claims.FirstOrDefault(c => c.Type == "role")?.Value,
        expectativa = new
        {
            alg = diag.Algoritmo,
            iss = diag.IssuerEsperado,
            aud = diag.AudienceEsperada,
        },
        bate_alg = string.Equals(parsed.Header.Alg, diag.Algoritmo, StringComparison.OrdinalIgnoreCase),
        bate_iss = string.Equals(parsed.Issuer, diag.IssuerEsperado, StringComparison.Ordinal),
        bate_aud = parsed.Audiences.Any(a => string.Equals(a, diag.AudienceEsperada, StringComparison.Ordinal)),
        validacao,
    });
});

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
