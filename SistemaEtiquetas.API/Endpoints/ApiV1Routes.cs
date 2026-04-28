using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.API.DTO;
using SistemaEtiquetas.API.Extensions;
using SistemaEtiquetas.API.Services;
using SistemaEtiquetas.Domain;
using SistemaEtiquetas.Domain.Entities;
using SistemaEtiquetas.Infrastructure.Data;
using System.Text.Json;

namespace SistemaEtiquetas.API.Endpoints;

public static class ApiV1Routes
{
    public static bool IsAuthConfigured(IConfiguration config) =>
        !string.IsNullOrEmpty(config["Auth:SupabaseJwtSecret"] ?? Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET"))
        && !string.IsNullOrEmpty(config["Auth:SupabaseUrl"] ?? Environment.GetEnvironmentVariable("SUPABASE_URL"));

    public static void MapApiV1(this WebApplication app, IConfiguration config)
    {
        var auth = IsAuthConfigured(config);
        var g = app.MapGroup("/api");
        if (auth)
            g = g.RequireAuthorization();

        g.MapGet("/config", async (AppDbContext db) =>
        {
            var c = await db.Configuracoes.AsNoTracking().FirstOrDefaultAsync();
            return Results.Ok(c);
        });

        g.MapPost("/config", async (AppDbContext db, Configuracao config) =>
        {
            var existente = await db.Configuracoes.FirstOrDefaultAsync();
            if (existente != null)
            {
                existente.StoreUrl = config.StoreUrl;
                existente.AccessToken = config.AccessToken;
                existente.RefreshToken = config.RefreshToken;
                existente.TokenExpiration = config.TokenExpiration;
                existente.SyncIntervalSeconds = config.SyncIntervalSeconds;
                existente.ImpressoraIp = config.ImpressoraIp;
                existente.ImpressoraPorta = config.ImpressoraPorta;
                await db.SaveChangesAsync();
                return Results.Ok(existente);
            }
            db.Configuracoes.Add(config);
            await db.SaveChangesAsync();
            return Results.Ok(config);
        });

        async Task<IResult> AuthSyncHandler(HttpContext ctx, AppDbContext db, IConfiguration cfg)
        {
            if (!auth)
                return Results.Json(new { message = "Autenticação não configurada na API" }, statusCode: 501);
            var u = await AuthUsuarioHelper.GetOrCreateUsuarioAsync(db, ctx.User, cfg, ctx.RequestAborted);
            if (u == null)
                return Results.Unauthorized();
            return Results.Ok(new
            {
                u.Id,
                u.Email,
                u.Nome,
                Perfil = u.Perfil.ToString(),
                u.Ativo
            });
        }

        g.MapPost("/auth/sync", AuthSyncHandler);
        g.MapGet("/auth/sync", AuthSyncHandler);

        g.MapGet("/pedidos", async (AppDbContext db, string? q, DateTime? data, int page = 1, int pageSize = 15) =>
        {
            if (page < 1) page = 1;
            if (pageSize is < 1 or > 100) pageSize = 15;

            IQueryable<Pedido> query = db.Pedidos.AsNoTracking().Include(p => p.Itens);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var t = q.Trim().ToLower();
                query = query.Where(p =>
                    (p.PedidoExternoId != null && p.PedidoExternoId.ToLower().Contains(t)) ||
                    (p.NomeCliente != null && p.NomeCliente.ToLower().Contains(t)) ||
                    (p.ClienteCpf != null && p.ClienteCpf.ToLower().Contains(t)));
            }

            if (data.HasValue)
            {
                // Filtro digitado pelo operador é dia-calendário em Brasília;
                // converte para o intervalo UTC equivalente.
                var dataFiltro = TimeZoneBrasil.DeBrasiliaParaUtc(data.Value.Date);
                var prox = TimeZoneBrasil.DeBrasiliaParaUtc(data.Value.Date.AddDays(1));
                query = query.Where(p => p.DataPedido >= dataFiltro && p.DataPedido < prox);
            }

            var pedidos = await query.OrderByDescending(p => p.DataPedido).ToListAsync();

            var rows = new List<PedidoItemRowDto>();
            foreach (var p in pedidos)
            {
                foreach (var it in p.Itens)
                {
                    rows.Add(new PedidoItemRowDto
                    {
                        PedidoId = p.Id,
                        PedidoItemId = it.Id,
                        DataPedido = p.DataPedido,
                        PedidoExternoId = p.PedidoExternoId ?? "",
                        NomeCliente = p.NomeCliente ?? "",
                        ClienteCpf = p.ClienteCpf,
                        Produto = it.Produto,
                        Cor = it.Cor,
                        Tamanho = it.Tamanho,
                        Quantidade = it.Quantidade,
                        Impresso = it.Impresso
                    });
                }
            }

            var total = rows.Count;
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            var paged = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            if (page > totalPages && totalPages > 0)
                paged = rows.Skip((totalPages - 1) * pageSize).Take(pageSize).ToList();

            return Results.Ok(new PagedResultDto<PedidoItemRowDto>
            {
                Items = paged,
                TotalCount = total,
                Page = page > totalPages && totalPages > 0 ? totalPages : page,
                PageSize = pageSize,
                TotalPages = Math.Max(1, totalPages)
            });
        });

        g.MapPost("/pedidos", async (CreatePedidoRequest body, AppDbContext db) =>
        {
            if (body?.Itens == null || !body.Itens.Any())
                return Results.BadRequest("Nenhum item no pedido.");

            var existente = await db.Pedidos.AnyAsync(p => p.PedidoExternoId == body.PedidoExternoId);
            if (existente)
                return Results.BadRequest("Já existe pedido com este ID externo.");

            var pedido = new Pedido
            {
                PedidoExternoId = body.PedidoExternoId,
                NomeCliente = body.NomeCliente,
                ClienteCpf = body.ClienteCpf,
                Vendedor = body.Vendedor ?? "Manual",
                TipoEnvio = body.TipoEnvio,
                FormaPagamento = body.FormaPagamento,
                ValorFrete = body.ValorFrete,
                DataPedido = TimeZoneBrasil.ParaUtcConsiderandoBrasilia(body.DataPedido),
                DataCriacao = DateTime.UtcNow
            };
            foreach (var it in body.Itens.Where(x => !string.IsNullOrWhiteSpace(x.Produto)))
            {
                // Regra: 1 linha = 1 etiqueta. A primeira linha leva os valores
                // monetários cheios; as cópias herdam ValorCusto/ValorVenda = 0
                // para preservar o total histórico do pedido.
                var n = it.Quantidade > 0 ? it.Quantidade : 1;
                for (var i = 0; i < n; i++)
                {
                    pedido.Itens.Add(new PedidoItem
                    {
                        Produto = it.Produto,
                        SKU = it.SKU ?? "",
                        Cor = it.Cor,
                        Tamanho = it.Tamanho,
                        Quantidade = 1,
                        ValorCusto = i == 0 ? it.ValorCusto : 0,
                        ValorVenda = i == 0 ? it.ValorVenda : 0
                    });
                }
            }
            db.Pedidos.Add(pedido);
            await db.SaveChangesAsync();
            return Results.Ok(pedido);
        });

        g.MapPut("/pedidos/{pedidoId:int}/itens/{itemId:int}", async (int pedidoId, int itemId, UpdatePedidoItemRequest body, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(body.Produto))
                return Results.BadRequest("Produto é obrigatório.");

            var pedido = await db.Pedidos.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == pedidoId);
            if (pedido == null) return Results.NotFound();

            var item = pedido.Itens.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return Results.NotFound();

            item.Produto = body.Produto;
            item.Cor = string.IsNullOrWhiteSpace(body.Cor) ? null : body.Cor;
            item.Tamanho = string.IsNullOrWhiteSpace(body.Tamanho) ? null : body.Tamanho;

            // Regra: 1 linha = 1 etiqueta. Se o operador editar para Quantidade > 1
            // mantemos a linha original com Qtd=1 (e os valores monetários originais)
            // e geramos N-1 cópias pendentes com ValorCusto/ValorVenda = 0.
            var n = body.Quantidade > 0 ? body.Quantidade : 1;
            item.Quantidade = 1;
            for (var i = 1; i < n; i++)
            {
                pedido.Itens.Add(new PedidoItem
                {
                    Produto = item.Produto,
                    SKU = item.SKU,
                    Cor = item.Cor,
                    Tamanho = item.Tamanho,
                    Quantidade = 1,
                    Impresso = false,
                    ValorCusto = 0,
                    ValorVenda = 0
                });
            }
            await db.SaveChangesAsync();
            return Results.Ok(item);
        });

        g.MapGet("/pedido-itens/{itemId:int}/zpl", async (int itemId, AppDbContext db) =>
        {
            var item = await db.PedidoItens.AsNoTracking().Include(i => i.Pedido).FirstOrDefaultAsync(i => i.Id == itemId);
            if (item == null) return Results.NotFound();

            var zpl = new EtiquetaService().GerarZpl(item.Pedido, item);
            return Results.Text(zpl, "text/plain; charset=utf-8");
        });

        g.MapGet("/pedido-itens/{itemId:int}/zpl.json", async (int itemId, AppDbContext db) =>
        {
            var item = await db.PedidoItens.AsNoTracking().Include(i => i.Pedido).FirstOrDefaultAsync(i => i.Id == itemId);
            if (item == null) return Results.NotFound();

            var zpl = new EtiquetaService().GerarZpl(item.Pedido, item);
            return Results.Json(new
            {
                zpl,
                item.Pedido.PedidoExternoId,
                item.Produto,
                item.Pedido.NomeCliente
            });
        });

        g.MapPost("/pedido-itens/{itemId:int}/marcar-impresso", async (int itemId, AppDbContext db) =>
        {
            var item = await db.PedidoItens.FirstOrDefaultAsync(i => i.Id == itemId);
            if (item == null) return Results.NotFound();
            item.Impresso = true;
            await db.SaveChangesAsync();
            return Results.Ok(new { item.Id, item.Impresso });
        });

        g.MapGet("/pedido-itens/pendentes-impressao", async (AppDbContext db, string? q, DateTime? data, string? ids) =>
        {
            var query = AplicarFiltrosPendentes(db.PedidoItens.AsNoTracking().Include(i => i.Pedido), q, data, ids);
            var list = await query
                .OrderBy(i => i.Id)
                .Select(i => new PendenteImpressaoDto
                {
                    ItemId = i.Id,
                    Quantidade = i.Quantidade > 0 ? i.Quantidade : 1
                })
                .ToListAsync();
            return Results.Ok(list);
        });

        // Lote de ZPL para impressão directa (QZ Tray). Cada elemento traz o
        // itemId e a string ZPL já formatada. O front envia para o QZ na ordem.
        // Aceita os mesmos filtros que a lista de pedidos (q, data) e ainda
        // ids=CSV de pedidoItemId quando o operador escolhe linhas específicas.
        g.MapGet("/pedido-itens/pendentes-impressao/zpl.json", async (AppDbContext db, string? q, DateTime? data, string? ids) =>
        {
            var query = AplicarFiltrosPendentes(db.PedidoItens.AsNoTracking().Include(i => i.Pedido), q, data, ids);
            var itens = await query.OrderBy(i => i.Id).ToListAsync();
            var svc = new EtiquetaService();
            var payload = itens.Select(it => new
            {
                itemId = it.Id,
                zpl = svc.GerarZpl(it.Pedido, it)
            });
            return Results.Json(payload);
        });

        g.MapGet("/relatorios/vendas", async (AppDbContext db, DateTime? inicio, DateTime? fim) =>
        {
            if (inicio == null || fim == null)
                return Results.BadRequest("Parâmetros inicio e fim são obrigatórios.");

            // Datas vêm como dia-calendário em Brasília; convertemos os limites
            // para UTC antes de filtrar contra a coluna timestamptz.
            var i0 = TimeZoneBrasil.DeBrasiliaParaUtc(inicio.Value.Date);
            var f0 = TimeZoneBrasil.DeBrasiliaParaUtc(fim.Value.Date.AddDays(1));

            var pedidos = await db.Pedidos.AsNoTracking()
                .Include(p => p.Itens)
                .Where(p => p.DataPedido >= i0 && p.DataPedido < f0)
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

            var linhas = new List<object>();
            foreach (var p in pedidos)
            {
                foreach (var it in p.Itens)
                {
                    linhas.Add(new
                    {
                        dataPedido = p.DataPedido,
                        sku = it.SKU ?? "N/A",
                        vendedor = p.Vendedor ?? "Manual",
                        peca = $"{it.Produto} - {it.Cor ?? "N/A"} - {it.Tamanho ?? "N/A"}",
                        cliente = p.NomeCliente,
                        tipoEnvio = p.TipoEnvio ?? "N/A",
                        valorCusto = it.ValorCusto,
                        valorVenda = it.ValorVenda,
                        formaPagamento = p.FormaPagamento ?? "N/A",
                        valorFrete = p.ValorFrete
                    });
                }
            }
            return Results.Json(linhas);
        });

        g.MapGet("/relatorios/vendas/export.xlsx", async (AppDbContext db, DateTime? inicio, DateTime? fim) =>
        {
            if (inicio == null || fim == null)
                return Results.BadRequest("Parâmetros inicio e fim são obrigatórios.");

            var i0 = TimeZoneBrasil.DeBrasiliaParaUtc(inicio.Value.Date);
            var f0 = TimeZoneBrasil.DeBrasiliaParaUtc(fim.Value.Date.AddDays(1));

            var pedidos = await db.Pedidos.AsNoTracking()
                .Include(p => p.Itens)
                .Where(p => p.DataPedido >= i0 && p.DataPedido < f0)
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

            if (!pedidos.Any())
                return Results.BadRequest("Nenhum pedido no período.");

            var file = RelatorioExcelService.GerarVendasExcel(pedidos, inicio.Value, fim.Value);
            var name = $"Relatorio_Vendas_{inicio:dd-MM-yyyy}_a_{fim:dd-MM-yyyy}.xlsx";
            return Results.File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
        });

        // Rotas administrativas ficam sempre registadas para que o cliente receba
        // 501 / 401 / 403 explícitos em vez de um 404 silencioso quando a
        // autenticação não está configurada no Render.
        var admin = g.MapGroup("/admin");
        admin.AddEndpointFilter(async (context, next) =>
        {
            if (!auth)
                return Results.Json(
                    new { error = "Autenticação não configurada na API (defina SUPABASE_URL e SUPABASE_JWT_SECRET)." },
                    statusCode: 501);

            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var id = AuthUsuarioHelper.GetUserId(context.HttpContext.User);
            if (id == null)
                return Results.Unauthorized();
            if (!await AuthUsuarioHelper.IsAdminAsync(db, id.Value, context.HttpContext.RequestAborted))
                return Results.Json(new { error = "Apenas administradores" }, statusCode: 403);
            return await next(context);
        });

        admin.MapGet("/usuarios", async (AppDbContext db) =>
        {
            var list = await db.Usuarios.AsNoTracking()
                .OrderByDescending(u => u.CriadoEm)
                .ToListAsync();
            return Results.Ok(list.Select(MapearUsuario));
        });

        admin.MapPatch("/usuarios/{id:guid}", async (Guid id, AtualizarUsuarioRequest body, AppDbContext db) =>
        {
            var u = await db.Usuarios.FindAsync(new object[] { id }, default);
            if (u == null) return Results.NotFound();

            if (body.Ativo.HasValue) u.Ativo = body.Ativo.Value;
            if (!string.IsNullOrEmpty(body.Perfil) && Enum.TryParse<UsuarioPerfil>(body.Perfil, true, out var perfil))
                u.Perfil = perfil;

            await db.SaveChangesAsync();
            return Results.Ok(MapearUsuario(u));
        });

        admin.MapPost("/usuarios/provision", async (ProvisionUsuarioRequest body, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(body.Email))
                return Results.BadRequest("E-mail é obrigatório.");
            if (!Enum.TryParse<UsuarioPerfil>(body.Perfil, true, out var perfil))
                return Results.BadRequest("Perfil inválido (use Operador ou Admin).");

            var existing = await db.Usuarios.FindAsync(new object[] { body.Id }, default);
            if (existing != null)
            {
                existing.Email = body.Email.Trim();
                existing.Perfil = perfil;
                if (!existing.Ativo) existing.Ativo = true;
            }
            else
            {
                db.Usuarios.Add(new UsuarioSistema
                {
                    Id = body.Id,
                    Email = body.Email.Trim(),
                    Perfil = perfil,
                    Ativo = true,
                    CriadoEm = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();
            var saved = await db.Usuarios.AsNoTracking().FirstAsync(u => u.Id == body.Id);
            return Results.Ok(MapearUsuario(saved));
        });
    }

    private static object MapearUsuario(UsuarioSistema u) => new
    {
        u.Id,
        u.Email,
        u.Nome,
        Perfil = u.Perfil.ToString(),
        u.Ativo,
        u.CriadoEm
    };

    // Aplica os mesmos filtros usados em /pedidos sobre a lista de itens
    // pendentes. Quando "ids" vem preenchido, ignora q/data e devolve só
    // os PedidoItem.Id contidos no CSV — inclui já impressos para permitir
    // reimpressão por selecção. Sem "ids", mantém só !Impresso.
    private static IQueryable<PedidoItem> AplicarFiltrosPendentes(
        IQueryable<PedidoItem> source,
        string? q,
        DateTime? data,
        string? ids)
    {
        if (!string.IsNullOrWhiteSpace(ids))
        {
            var idList = ids
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var n) ? n : 0)
                .Where(n => n > 0)
                .Distinct()
                .ToList();
            if (idList.Count == 0)
                return source.Where(_ => false);
            return source.Where(i => idList.Contains(i.Id));
        }

        var query = source.Where(i => !i.Impresso);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim().ToLower();
            query = query.Where(i =>
                (i.Pedido.PedidoExternoId != null && i.Pedido.PedidoExternoId.ToLower().Contains(t)) ||
                (i.Pedido.NomeCliente != null && i.Pedido.NomeCliente.ToLower().Contains(t)) ||
                (i.Pedido.ClienteCpf != null && i.Pedido.ClienteCpf.ToLower().Contains(t)));
        }

        if (data.HasValue)
        {
            var dataFiltro = TimeZoneBrasil.DeBrasiliaParaUtc(data.Value.Date);
            var prox = TimeZoneBrasil.DeBrasiliaParaUtc(data.Value.Date.AddDays(1));
            query = query.Where(i => i.Pedido.DataPedido >= dataFiltro && i.Pedido.DataPedido < prox);
        }

        return query;
    }

}
