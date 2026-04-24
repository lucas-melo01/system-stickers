using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace SistemaEtiquetas.API.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>Estado carregado no arranque, para /diag/auth apresentar informação útil sem expor o segredo.</summary>
    public sealed class AuthDiagSnapshot
    {
        public bool Configurado { get; set; }
        public string? SupabaseUrl { get; set; }
        public int SecretTamanho { get; set; }
        public string? SecretUltimos4 { get; set; }
        public string? IssuerEsperado { get; set; }
        public string AudienceEsperada { get; set; } = "authenticated";
        public string Algoritmo { get; set; } = "HS256";
    }

    public static AuthDiagSnapshot AuthDiag { get; } = new();

    public static void AddSistemaEtiquetasAuth(this IServiceCollection services, IConfiguration config)
    {
        var secretRaw = config["Auth:SupabaseJwtSecret"] ?? Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET");
        var urlRaw = config["Auth:SupabaseUrl"] ?? Environment.GetEnvironmentVariable("SUPABASE_URL");

        // Trim defensivo: valores copiados do painel Render podem trazer espaço/aspas/quebra de linha.
        var secret = (secretRaw ?? "").Trim().Trim('"', '\'');
        var baseUrl = (urlRaw ?? "").Trim().Trim('"', '\'').TrimEnd('/');

        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(baseUrl))
        {
            Console.WriteLine("⚠️ Auth: defina Auth:SupabaseUrl e Auth:SupabaseJwtSecret (ou SUPABASE_URL / SUPABASE_JWT_SECRET). API /api/* ficará pública sem JWT.");
            AuthDiag.Configurado = false;
            AuthDiag.SupabaseUrl = baseUrl;
            return;
        }

        AuthDiag.Configurado = true;
        AuthDiag.SupabaseUrl = baseUrl;
        AuthDiag.SecretTamanho = secret.Length;
        AuthDiag.SecretUltimos4 = secret.Length >= 4 ? secret[^4..] : "****";
        AuthDiag.IssuerEsperado = $"{baseUrl}/auth/v1";

        Console.WriteLine($"🔐 Auth HS256 carregada. issuer={AuthDiag.IssuerEsperado} secretLen={AuthDiag.SecretTamanho}");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                    ValidIssuer = $"{baseUrl}/auth/v1",
                    ValidAudience = "authenticated",
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2)
                };

                // Logs detalhados ao falhar a autenticação; aparecem nos logs do Render e
                // permitem distinguir "alg errado" de "assinatura inválida" / "expirado".
                o.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        var logger = ctx.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtBearer");
                        logger.LogWarning(ctx.Exception,
                            "JWT inválido: {Mensagem}", ctx.Exception.Message);
                        ctx.Response.Headers["x-auth-error"] = ctx.Exception.GetType().Name;
                        return Task.CompletedTask;
                    },
                    OnChallenge = ctx =>
                    {
                        if (!string.IsNullOrEmpty(ctx.ErrorDescription))
                            ctx.Response.Headers["x-auth-error-description"] = ctx.ErrorDescription;
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
    }

    /// <summary>
    /// Lista estática das origens carregadas, para diagnóstico em /diag/cors.
    /// </summary>
    public static IReadOnlyList<string> CorsOrigens { get; private set; } = Array.Empty<string>();

    public static void AddSistemaEtiquetasCors(this IServiceCollection services, IConfiguration config)
    {
        // CORS_ORIGINS (env) tem prioridade sobre Cors:Origins (appsettings).
        // Em produção o appsettings inclui localhost; sem a env, o browser não recebia
        // Access-Control-Allow-Origin para o domínio do Vercel.
        var raw = Environment.GetEnvironmentVariable("CORS_ORIGINS");
        if (string.IsNullOrWhiteSpace(raw))
            raw = config["Cors:Origins"];
        if (string.IsNullOrWhiteSpace(raw))
            raw = "http://localhost:3000,http://127.0.0.1:3000";

        var entradas = raw
            .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(o => o.TrimEnd('/'))
            .Where(o => o.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var exatas = entradas.Where(o => !o.Contains('*')).ToArray();
        var padroes = entradas.Where(o => o.Contains('*')).ToArray();

        CorsOrigens = entradas;
        Console.WriteLine($"🌐 CORS habilitado para: {string.Join(", ", entradas)}");

        services.AddCors(o =>
        {
            o.AddPolicy("Web", p =>
            {
                if (exatas.Length > 0)
                    p.WithOrigins(exatas);

                if (padroes.Length > 0)
                {
                    p.SetIsOriginAllowed(origin =>
                    {
                        if (string.IsNullOrWhiteSpace(origin)) return false;
                        var normalizado = origin.TrimEnd('/');
                        foreach (var padrao in padroes)
                        {
                            if (OrigemCombinaPadrao(normalizado, padrao)) return true;
                        }
                        return false;
                    });
                }

                p.AllowAnyHeader()
                 .AllowAnyMethod();
            });
        });
    }

    private static bool OrigemCombinaPadrao(string origin, string padrao)
    {
        // Suporta apenas curinga no subdomínio, tipo "https://*.vercel.app".
        // Isso evita depender de Regex genérico e acidentalmente autorizar domínios inesperados.
        var idxProtocol = padrao.IndexOf("://", StringComparison.Ordinal);
        if (idxProtocol < 0) return false;
        var idxStar = padrao.IndexOf('*', idxProtocol);
        if (idxStar < 0)
            return string.Equals(origin, padrao, StringComparison.OrdinalIgnoreCase);

        var prefixo = padrao[..idxStar];
        var sufixo = padrao[(idxStar + 1)..];
        if (!origin.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase)) return false;
        if (!origin.EndsWith(sufixo, StringComparison.OrdinalIgnoreCase)) return false;
        var meio = origin.Substring(prefixo.Length, origin.Length - prefixo.Length - sufixo.Length);
        if (meio.Length == 0) return false;
        return !meio.Contains('/') && !meio.Contains(':');
    }
}
