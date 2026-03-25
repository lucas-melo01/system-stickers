using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.Domain.Entities;
using SistemaEtiquetas.Infrastructure;
using SistemaEtiquetas.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=database.db"));

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
    var pedidos = await db.Pedidos.ToListAsync();
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

app.MapGet("/", () => "API Rodando 🚀");

app.Run();