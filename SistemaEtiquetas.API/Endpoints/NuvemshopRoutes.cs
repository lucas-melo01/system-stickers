using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SistemaEtiquetas.API.DTO;
using SistemaEtiquetas.API.Extensions;
using SistemaEtiquetas.API.Services;
using SistemaEtiquetas.Domain.Entities;
using SistemaEtiquetas.Infrastructure.Data;

namespace SistemaEtiquetas.API.Endpoints;

public static class NuvemshopRoutes
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static void MapNuvemshop(this WebApplication app)
    {
        app.MapGet("/oauth/nuvemshop/install", Install);
        app.MapGet("/oauth/nuvemshop/callback", Callback);
        app.MapPost("/webhook/nuvemshop", Webhook);
        app.MapPost("/webhook/nuvemshop/privacy/store-redact", StoreRedact);
        app.MapPost("/webhook/nuvemshop/privacy/customer-redact", CustomerRedact);
        app.MapPost("/webhook/nuvemshop/privacy/customers-data-request", CustomersDataRequest);
    }

    private static IResult Install(HttpContext http, IOptions<NuvemshopOptions> opt)
    {
        var loja = http.Request.Query["loja"].FirstOrDefault() ?? "resumemodas";
        var state = NuvemshopLojaMapper.ResolveVendedor(loja) == "DonnaKora"
            ? "donnakora"
            : "resumemodas";

        if (string.IsNullOrWhiteSpace(opt.Value.AppId))
            return Results.BadRequest("Nuvemshop AppId não configurado");

        var auth = http.RequestServices.GetRequiredService<NuvemshopAuthService>();
        return Results.Redirect(auth.BuildAuthorizeUrl(state));
    }

    private static async Task<IResult> Callback(
        HttpContext http,
        AppDbContext db,
        NuvemshopAuthService auth,
        NuvemshopApiClient api,
        IOptions<NuvemshopOptions> opt,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("NuvemshopOAuth");
        var code = http.Request.Query["code"].FirstOrDefault();
        var state = http.Request.Query["state"].FirstOrDefault()
            ?? http.Request.Query["loja"].FirstOrDefault()
            ?? "resumemodas";

        if (string.IsNullOrWhiteSpace(code))
            return Results.BadRequest("Parâmetro code ausente");

        var token = await auth.ExchangeCodeAsync(code);
        if (token?.AccessToken == null)
            return Results.BadRequest("Falha ao obter access_token da Nuvemshop");

        var storeId = NuvemshopAuthService.ResolveStoreId(token);
        if (storeId <= 0)
            return Results.BadRequest("user_id/store_id ausente na resposta OAuth");

        var vendedor = NuvemshopLojaMapper.ResolveVendedor(state);
        var now = DateTime.UtcNow;
        var instalacao = await db.NuvemshopInstalacoes.FirstOrDefaultAsync(i => i.StoreId == storeId);
        if (instalacao == null)
        {
            instalacao = new NuvemshopInstalacao
            {
                StoreId = storeId,
                AccessToken = token.AccessToken,
                Vendedor = vendedor,
                Scopes = token.Scope,
                Ativo = true,
                InstalledAt = now,
                UpdatedAt = now,
            };
            db.NuvemshopInstalacoes.Add(instalacao);
        }
        else
        {
            instalacao.AccessToken = token.AccessToken;
            instalacao.Vendedor = vendedor;
            instalacao.Scopes = token.Scope;
            instalacao.Ativo = true;
            instalacao.UpdatedAt = now;
        }

        await db.SaveChangesAsync();

        var publicBase = ResolvePublicBaseUrl(http, opt.Value);
        if (!string.IsNullOrWhiteSpace(publicBase))
        {
            try
            {
                await api.EnsureWebhooksAsync(storeId, token.AccessToken, publicBase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao registrar webhooks Nuvemshop store={StoreId}", storeId);
            }
        }
        else
        {
            logger.LogWarning("Nuvemshop PublicBaseUrl ausente; webhooks não registrados automaticamente");
        }

        var html = $"""
            <!DOCTYPE html>
            <html lang="pt-BR"><head><meta charset="utf-8"><title>Nuvemshop</title></head>
            <body style="font-family:system-ui;padding:2rem">
              <h1>App instalado</h1>
              <p>Loja <strong>{storeId}</strong> vinculada como <strong>{vendedor}</strong>.</p>
              <p>Webhooks de pedido pago foram configurados{(string.IsNullOrWhiteSpace(publicBase) ? " (configure Nuvemshop__PublicBaseUrl e reinstale)" : "")}.</p>
            </body></html>
            """;
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static async Task<IResult> Webhook(
        HttpContext http,
        AppDbContext db,
        NuvemshopApiClient api,
        IOptions<NuvemshopOptions> opt,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("NuvemshopWebhook");
        var raw = await ReadBodyAsync(http);
        if (!NuvemshopHmacHelper.Validate(opt, raw, http.Request.Headers))
            return Results.Unauthorized();

        NuvemshopWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<NuvemshopWebhookPayload>(raw, JsonOpts);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Payload webhook Nuvemshop inválido");
            return Results.Ok("ignored");
        }

        if (payload == null || string.IsNullOrWhiteSpace(payload.Event))
            return Results.Ok("ignored");

        var eventName = payload.Event.Trim();
        var storeId = payload.StoreId;

        if (string.Equals(eventName, "order/paid", StringComparison.OrdinalIgnoreCase))
        {
            var orderId = NuvemshopJsonHelpers.ParseLong(payload.Id);
            if (orderId <= 0)
                return Results.Ok("order id inválido");
            return await NuvemshopPedidoHandler.ProcessarOrderPaidAsync(
                storeId, orderId, db, api, logger, http.RequestAborted);
        }

        if (string.Equals(eventName, "app/uninstalled", StringComparison.OrdinalIgnoreCase))
        {
            return await NuvemshopPedidoHandler.ProcessarAppUninstalledAsync(
                storeId, db, logger, http.RequestAborted);
        }

        if (string.Equals(eventName, "app/suspended", StringComparison.OrdinalIgnoreCase)
            || string.Equals(eventName, "app/resumed", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Nuvemshop {Event} store={StoreId}", eventName, storeId);
            return Results.Ok("ok");
        }

        return Results.Ok("ignored");
    }

    private static async Task<IResult> StoreRedact(
        HttpContext http,
        AppDbContext db,
        IOptions<NuvemshopOptions> opt,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("NuvemshopPrivacy");
        var raw = await ReadBodyAsync(http);
        if (!NuvemshopHmacHelper.Validate(opt, raw, http.Request.Headers))
            return Results.Unauthorized();

        var storeId = TryReadStoreId(raw);
        logger.LogInformation("store-redact store={StoreId} payload={Payload}", storeId, Truncate(raw));

        if (storeId > 0)
        {
            var instalacao = await db.NuvemshopInstalacoes
                .FirstOrDefaultAsync(i => i.StoreId == storeId);
            if (instalacao != null)
            {
                instalacao.Ativo = false;
                instalacao.AccessToken = "";
                instalacao.UpdatedAt = DateTime.UtcNow;

                var prefix = $"ns:{storeId}:";
                var pedidos = await db.Pedidos
                    .Where(p => p.PedidoExternoId.StartsWith(prefix))
                    .ToListAsync();
                foreach (var p in pedidos)
                {
                    p.NomeCliente = "REDACTED";
                    p.ClienteCpf = null;
                }

                await db.SaveChangesAsync();
            }
        }

        return Results.Ok();
    }

    private static async Task<IResult> CustomerRedact(
        HttpContext http,
        AppDbContext db,
        IOptions<NuvemshopOptions> opt,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("NuvemshopPrivacy");
        var raw = await ReadBodyAsync(http);
        if (!NuvemshopHmacHelper.Validate(opt, raw, http.Request.Headers))
            return Results.Unauthorized();

        logger.LogInformation("customer-redact payload={Payload}", Truncate(raw));

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
        var root = doc.RootElement;
        var storeId = root.TryGetProperty("store_id", out var sid)
            ? NuvemshopJsonHelpers.ParseLong(sid)
            : 0;

        string? email = null;
        string? identification = null;
        if (root.TryGetProperty("customer", out var customer))
        {
            if (customer.TryGetProperty("email", out var e)) email = e.GetString();
            if (customer.TryGetProperty("identification", out var idn)) identification = idn.GetString();
            if (identification == null && customer.TryGetProperty("contact_identification", out var cid))
                identification = cid.GetString();
        }

        if (!string.IsNullOrWhiteSpace(identification))
        {
            IQueryable<Pedido> q = db.Pedidos.Where(p => p.PedidoExternoId.StartsWith("ns:"));
            if (storeId > 0)
            {
                var prefix = $"ns:{storeId}:";
                q = q.Where(p => p.PedidoExternoId.StartsWith(prefix));
            }

            var pedidos = await q
                .Where(p => p.ClienteCpf == identification)
                .ToListAsync();
            foreach (var p in pedidos)
            {
                p.NomeCliente = "REDACTED";
                p.ClienteCpf = null;
            }

            await db.SaveChangesAsync();
            logger.LogInformation(
                "customer-redact anonimizados={Count} store={StoreId} email={Email}",
                pedidos.Count, storeId, email);
        }

        return Results.Ok();
    }

    private static async Task<IResult> CustomersDataRequest(
        HttpContext http,
        AppDbContext db,
        IOptions<NuvemshopOptions> opt,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("NuvemshopPrivacy");
        var raw = await ReadBodyAsync(http);
        if (!NuvemshopHmacHelper.Validate(opt, raw, http.Request.Headers))
            return Results.Unauthorized();

        logger.LogInformation("customers-data-request payload={Payload}", Truncate(raw));

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
        var root = doc.RootElement;
        var storeId = root.TryGetProperty("store_id", out var sid)
            ? NuvemshopJsonHelpers.ParseLong(sid)
            : 0;

        string? identification = null;
        if (root.TryGetProperty("customer", out var customer))
        {
            if (customer.TryGetProperty("identification", out var idn)) identification = idn.GetString();
            if (identification == null && customer.TryGetProperty("contact_identification", out var cid))
                identification = cid.GetString();
        }

        IQueryable<Pedido> q = db.Pedidos.Where(p => p.PedidoExternoId.StartsWith("ns:"));
        if (storeId > 0)
        {
            var prefix = $"ns:{storeId}:";
            q = q.Where(p => p.PedidoExternoId.StartsWith(prefix));
        }

        if (!string.IsNullOrWhiteSpace(identification))
            q = q.Where(p => p.ClienteCpf == identification);

        var dados = await q
            .Select(p => new
            {
                p.PedidoExternoId,
                p.NomeCliente,
                p.ClienteCpf,
                p.DataPedido,
                p.Vendedor,
            })
            .Take(500)
            .ToListAsync();

        logger.LogInformation("customers-data-request encontrados={Count}", dados.Count);
        // LGPD: a plataforma espera ACK; o relatório pode ser entregue offline pelo suporte.
        return Results.Ok(new { received = true, count = dados.Count });
    }

    private static string ResolvePublicBaseUrl(HttpContext http, NuvemshopOptions opt)
    {
        if (!string.IsNullOrWhiteSpace(opt.PublicBaseUrl))
            return opt.PublicBaseUrl.TrimEnd('/');

        var env = Environment.GetEnvironmentVariable("NUVEMSHOP_PUBLIC_BASE_URL")
            ?? Environment.GetEnvironmentVariable("PUBLIC_BASE_URL");
        if (!string.IsNullOrWhiteSpace(env))
            return env.TrimEnd('/');

        // Fallback: scheme + host da request (útil se o callback já chega pelo domínio público)
        if (http.Request.Host.HasValue)
            return $"{http.Request.Scheme}://{http.Request.Host}".TrimEnd('/');

        return "";
    }

    private static async Task<string> ReadBodyAsync(HttpContext http)
    {
        using var reader = new StreamReader(http.Request.Body);
        return await reader.ReadToEndAsync();
    }

    private static long TryReadStoreId(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
            if (doc.RootElement.TryGetProperty("store_id", out var sid))
                return NuvemshopJsonHelpers.ParseLong(sid);
        }
        catch
        {
            // ignore
        }

        return 0;
    }

    private static string Truncate(string? s, int max = 500) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..max];
}
