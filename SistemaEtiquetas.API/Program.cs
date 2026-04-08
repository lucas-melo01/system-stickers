using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.API.DTO;
using SistemaEtiquetas.API.Services;
using SistemaEtiquetas.Domain.Entities;
using SistemaEtiquetas.Infrastructure;
using SistemaEtiquetas.Infrastructure.Data;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=database.db"));

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

var app = builder.Build();

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
    
    return Results.Ok(pedidos);
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

app.MapPost("/webhook/pedido", async (AppDbContext db, HttpRequest request) =>
{
    var payload = await new StreamReader(request.Body).ReadToEndAsync();

    var options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    var pedidoDto = JsonSerializer.Deserialize<WebhookPedidoDto>(payload, options);

    // 🔒 evita duplicidade
    var existe = await db.Pedidos
        .AnyAsync(p => p.PedidoExternoId == pedidoDto.id.ToString());

    if (existe)
        return Results.Ok("Pedido já processado");

    var pedido = new Pedido
    {
        PedidoExternoId = pedidoDto.id.ToString(),
        NomeCliente = pedidoDto.cliente?.nome,
        DataPedido = pedidoDto.data_criacao
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

app.MapPost("/imprimir", async (AppDbContext db) =>
{
    var pedidos = await db.Pedidos
        .Include(p => p.Itens)
        .ToListAsync();

    var etiquetaService = new EtiquetaService();
    var impressaoService = new ImpressaoService();

    foreach (var pedido in pedidos)
    {
        foreach (var item in pedido.Itens.Where(i => !i.Impresso))
        {
            try
            {
                for (int i = 0; i < item.Quantidade; i++)
                {
                    var zpl = etiquetaService.GerarZpl(pedido, item);

                    var sucesso = impressaoService.Imprimir(zpl, out string erro);

                    if (!sucesso)
                        throw new Exception(erro);
                }

                item.Impresso = true;
                item.Erro = false;
                item.ErroMensagem = null;
            }
            catch (Exception ex)
            {
                item.Erro = true;
                item.ErroMensagem = ex.Message;
            }
        }
    }

    await db.SaveChangesAsync();

    return Results.Ok("Processamento finalizado");
});

app.MapPost("/reimprimir/{itemId}", async (AppDbContext db, int itemId) =>
{
    var item = await db.PedidoItens
        .Include(i => i.Pedido)
        .FirstOrDefaultAsync(i => i.Id == itemId);

    if (item == null)
        return Results.NotFound();

    var etiquetaService = new EtiquetaService();
    var impressaoService = new ImpressaoService();

    for (int i = 0; i < item.Quantidade; i++)
    {
        var zpl = etiquetaService.GerarZpl(item.Pedido, item);
        impressaoService.Imprimir(zpl);
    }

    return Results.Ok("Reimpressão feita");
});

app.MapGet("/etiqueta/teste", async (AppDbContext db) =>
{
    var pedido = await db.Pedidos.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == 3);

    if (pedido == null || !pedido.Itens.Any())
        return Results.BadRequest("Sem dados");

    var service = new EtiquetaService();

    var zpl = service.GerarZpl(pedido, pedido.Itens.First());

    return Results.Text(zpl, "text/plain");
});

app.MapGet("/", () => "API Rodando 🚀");

app.Run();