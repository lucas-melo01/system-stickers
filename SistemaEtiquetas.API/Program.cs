using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.API.DTO;
using SistemaEtiquetas.API.Services;
using SistemaEtiquetas.Domain.Entities;
using SistemaEtiquetas.Infrastructure;
using SistemaEtiquetas.Infrastructure.Data;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure Postgres (Supabase) connection. Prefer environment variable "DATABASE_URL", fallback to appsettings.json
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(databaseUrl));

// Adicionar CORS para permitir requisições de qualquer origem em desenvolvimento
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// registra ImpressaoService no DI para ler config.json
builder.Services.AddSingleton<ImpressaoService>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

var app = builder.Build();

// Usar CORS
app.UseCors("AllowAll");

// Apply any pending migrations to ensure database schema matches model (creates identity columns etc.)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch
    {
        // Migration failures should be visible in logs during startup; swallow here to avoid crashing in debugging scenarios
    }
}

_ = Task.Run(async () =>
{
    while (true)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var etiquetaService = new EtiquetaService();
        var impressaoService = scope.ServiceProvider.GetRequiredService<ImpressaoService>();

        var itensPendentes = await db.PedidoItens
            .Include(i => i.Pedido)
            .Where(i => !i.Impresso)
            .ToListAsync();

        foreach (var item in itensPendentes)
        {
            try
            {
                for (int i = 0; i < item.Quantidade; i++)
                {
                    var zpl = etiquetaService.GerarZpl(item.Pedido, item);

                    var sucesso = impressaoService.Imprimir(zpl, out string erro);

                    if (!sucesso)
                        throw new Exception(erro);
                }

                item.Impresso = true;
            }
            catch (Exception ex)
            {
                // Error handling: log internally if needed, but don't persist to database
            }
        }

        await db.SaveChangesAsync();

        await Task.Delay(TimeSpan.FromMinutes(2)); // roda a cada 2 min
    }
});

app.MapPost("/pedidos", async (AppDbContext db, Pedido pedido) =>
{
    var existe = await db.Pedidos
        .AnyAsync(p => p.PedidoExternoId == pedido.PedidoExternoId);

    if (existe)
        return Results.BadRequest("Pedido já existe");

    db.Pedidos.Add(pedido);
    await db.SaveChangesAsync();

    return Results.Ok(pedido);
});

app.MapGet("/pedidos", async (AppDbContext db) =>
{
    var pedidos = await db.Pedidos.Include(p => p.Itens).ToListAsync();

    if (!pedidos.Any())
    {
        return Results.Ok(new { message = "Nenhum pedido encontrado", data = pedidos });
    }

    return Results.Ok(new { message = $"Total de {pedidos.Count} pedidos", data = pedidos });
});

app.MapPost("/config", async (AppDbContext db, Configuracao config) =>
{
    var existente = await db.Configuracoes.FirstOrDefaultAsync();

    if (existente != null)
    {
        existente.StoreUrl = config.StoreUrl;
        existente.AccessToken = config.AccessToken;
        existente.RefreshToken = config.RefreshToken;
        existente.TokenExpiration = config.TokenExpiration;
        existente.SyncIntervalSeconds = config.SyncIntervalSeconds;

        await db.SaveChangesAsync();
        return Results.Ok(existente);
    }

    db.Configuracoes.Add(config);
    await db.SaveChangesAsync();

    return Results.Ok(config);
});

app.MapGet("/config", async (AppDbContext db) =>
{
    var config = await db.Configuracoes.FirstOrDefaultAsync();
    return Results.Ok(config);
});


// Webhook Resume Modas
app.MapPost("/webhook/pedido/resumemodas", async (AppDbContext db, HttpRequest request) =>
{
    var payload = await new StreamReader(request.Body).ReadToEndAsync();
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    var pedidoDto = JsonSerializer.Deserialize<WebhookPedidoDto>(payload, options);
    var existe = await db.Pedidos.AnyAsync(p => p.PedidoExternoId == pedidoDto.id.ToString());
    if (existe) return Results.Ok("Pedido já processado");
    var dataPedido = pedidoDto.data_criacao;
    if (dataPedido.Kind == DateTimeKind.Unspecified)
        dataPedido = DateTime.SpecifyKind(dataPedido, DateTimeKind.Utc);
    dataPedido = dataPedido.ToUniversalTime();
    var tipoEnvio = pedidoDto.envios?.FirstOrDefault()?.forma_envio?.nome ?? "N/A";
    var valorFrete = pedidoDto.envios?.FirstOrDefault()?.valor ?? pedidoDto.valor_envio;
    var formaPagamento = pedidoDto.pagamentos?.FirstOrDefault()?.forma_pagamento?.nome ?? "N/A";
    var pedido = new Pedido
    {
        PedidoExternoId = pedidoDto.id.ToString(),
        NomeCliente = pedidoDto.cliente?.nome,
        DataPedido = dataPedido,
        ClienteCpf = pedidoDto.cliente?.cpf,
        Vendedor = "Resume Modas",
        TipoEnvio = tipoEnvio,
        FormaPagamento = formaPagamento,
        ValorFrete = valorFrete
    };
    foreach (var item in pedidoDto.itens)
    {
        pedido.Itens.Add(new PedidoItem
        {
            Produto = item.nome,
            SKU = item.sku,
            Quantidade = item.quantidade
        });
    }
    db.Pedidos.Add(pedido);
    await db.SaveChangesAsync();
    return Results.Ok("Pedido salvo com sucesso");
});

// Webhook DonnaKora
app.MapPost("/webhook/pedido/donnakora", async (AppDbContext db, HttpRequest request) =>
{
    var payload = await new StreamReader(request.Body).ReadToEndAsync();
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    var pedidoDto = JsonSerializer.Deserialize<WebhookPedidoDto>(payload, options);
    var existe = await db.Pedidos.AnyAsync(p => p.PedidoExternoId == pedidoDto.id.ToString());
    if (existe) return Results.Ok("Pedido já processado");
    var dataPedido = pedidoDto.data_criacao;
    if (dataPedido.Kind == DateTimeKind.Unspecified)
        dataPedido = DateTime.SpecifyKind(dataPedido, DateTimeKind.Utc);
    dataPedido = dataPedido.ToUniversalTime();
    var tipoEnvio = pedidoDto.envios?.FirstOrDefault()?.forma_envio?.nome ?? "N/A";
    var valorFrete = pedidoDto.envios?.FirstOrDefault()?.valor ?? pedidoDto.valor_envio;
    var formaPagamento = pedidoDto.pagamentos?.FirstOrDefault()?.forma_pagamento?.nome ?? "N/A";
    var pedido = new Pedido
    {
        PedidoExternoId = pedidoDto.id.ToString(),
        NomeCliente = pedidoDto.cliente?.nome,
        DataPedido = dataPedido,
        ClienteCpf = pedidoDto.cliente?.cpf,
        Vendedor = "DonnaKora",
        TipoEnvio = tipoEnvio,
        FormaPagamento = formaPagamento,
        ValorFrete = valorFrete
    };
    foreach (var item in pedidoDto.itens)
    {
        pedido.Itens.Add(new PedidoItem
        {
            Produto = item.nome,
            SKU = item.sku,
            Quantidade = item.quantidade
        });
    }
    db.Pedidos.Add(pedido);
    await db.SaveChangesAsync();
    return Results.Ok("Pedido salvo com sucesso");
});

app.MapPost("/reimprimir/{itemId}", async (AppDbContext db, int itemId) =>
{
    var item = await db.PedidoItens
        .Include(i => i.Pedido)
        .FirstOrDefaultAsync(i => i.Id == itemId);

    if (item == null)
        return Results.NotFound();

    var etiquetaService = new EtiquetaService();
    var impressaoService = app.Services.GetRequiredService<ImpressaoService>();

    for (int i = 0; i < item.Quantidade; i++)
    {
        var zpl = etiquetaService.GerarZpl(item.Pedido, item);
        impressaoService.Imprimir(zpl, out _);
    }

    return Results.Ok("Reimpressão feita");
});

app.MapGet("/", () => "API Rodando 🚀");

app.MapGet("/health", (AppDbContext db) =>
{
    try
    {
        var canConnect = db.Database.CanConnect();

        return canConnect 
            ? Results.Ok(new { status = "Healthy ✅", timestamp = DateTime.UtcNow })
            : Results.StatusCode(503);
    }
    catch
    {
        return Results.StatusCode(503);
    }
});

app.Run();