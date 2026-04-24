using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace SistemaEtiquetas.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddSistemaEtiquetasAuth(this IServiceCollection services, IConfiguration config)
    {
        var secret = config["Auth:SupabaseJwtSecret"] ?? Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET");
        var baseUrl = (config["Auth:SupabaseUrl"] ?? Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "").TrimEnd('/');

        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(baseUrl))
        {
            Console.WriteLine("⚠️ Auth: defina Auth:SupabaseUrl e Auth:SupabaseJwtSecret (ou SUPABASE_URL / SUPABASE_JWT_SECRET). API /api/* ficará pública sem JWT.");
            return;
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidIssuer = $"{baseUrl}/auth/v1",
                    ValidAudience = "authenticated",
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2)
                };
            });

        services.AddAuthorization();
    }

    public static void AddSistemaEtiquetasCors(this IServiceCollection services, IConfiguration config)
    {
        // CORS_ORIGINS tem prioridade: em produção o appsettings traz ainda "localhost" e,
        // como Cors:Origins nunca fica vazio, a variável CORS_ORIGINS (Render) era ignorada
        // e o browser não recebia Access-Control-Allow-Origin para o domínio do Vercel.
        var raw = Environment.GetEnvironmentVariable("CORS_ORIGINS");
        if (string.IsNullOrWhiteSpace(raw))
            raw = config["Cors:Origins"];
        if (string.IsNullOrWhiteSpace(raw))
            raw = "http://localhost:3000,http://127.0.0.1:3000";

        var origins = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(o => o.TrimEnd('/'))
            .Where(o => o.Length > 0)
            .Distinct()
            .ToArray();

        services.AddCors(o =>
        {
            o.AddPolicy("Web", p => p
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod());
        });
    }
}
