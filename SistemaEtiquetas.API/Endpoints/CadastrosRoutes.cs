using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.API.DTO;
using SistemaEtiquetas.Domain;
using SistemaEtiquetas.Domain.Entities;
using SistemaEtiquetas.Infrastructure.Data;

namespace SistemaEtiquetas.API.Endpoints;

public static class CadastrosRoutes
{
    public static void MapCadastrosRoutes(this RouteGroupBuilder g)
    {
        // ─── Fornecedores ───
        g.MapGet("/fornecedores", async (AppDbContext db, string? q, int page = 1, int pageSize = 50) =>
        {
            if (page < 1) page = 1;
            if (pageSize is < 1 or > 100) pageSize = 50;

            var query = db.Fornecedores.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var t = q.Trim().ToLower();
                query = query.Where(f =>
                    f.NomeRazaoSocial.ToLower().Contains(t) ||
                    (f.Email != null && f.Email.ToLower().Contains(t)) ||
                    f.WhatsApp.Contains(t));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(f => f.NomeRazaoSocial)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FornecedorDto
                {
                    Id = f.Id,
                    NomeRazaoSocial = f.NomeRazaoSocial,
                    Email = f.Email,
                    WhatsApp = f.WhatsApp,
                    Ativo = f.Ativo,
                    ProdutosVinculados = db.Produtos.Count(p => p.FornecedorId == f.Id && p.Ativo),
                    CriadoEm = f.CriadoEm,
                    AtualizadoEm = f.AtualizadoEm,
                })
                .ToListAsync();

            return Results.Ok(new PagedResultDto<FornecedorDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            });
        });

        g.MapGet("/fornecedores/{id:int}", async (int id, AppDbContext db) =>
        {
            var f = await db.Fornecedores.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (f == null) return Results.NotFound();
            var count = await db.Produtos.CountAsync(p => p.FornecedorId == id);
            return Results.Ok(new FornecedorDto
            {
                Id = f.Id,
                NomeRazaoSocial = f.NomeRazaoSocial,
                Email = f.Email,
                WhatsApp = f.WhatsApp,
                Ativo = f.Ativo,
                ProdutosVinculados = count,
                CriadoEm = f.CriadoEm,
                AtualizadoEm = f.AtualizadoEm,
            });
        });

        g.MapPost("/fornecedores", async (CreateFornecedorRequest body, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(body.NomeRazaoSocial))
                return Results.BadRequest("Nome/Razão Social é obrigatório.");
            if (string.IsNullOrWhiteSpace(body.WhatsApp))
                return Results.BadRequest("WhatsApp é obrigatório.");

            var f = new Fornecedor
            {
                NomeRazaoSocial = body.NomeRazaoSocial.Trim(),
                Email = string.IsNullOrWhiteSpace(body.Email) ? null : body.Email.Trim(),
                WhatsApp = NormalizarWhatsApp(body.WhatsApp),
                Ativo = body.Ativo,
                CriadoEm = DateTime.UtcNow,
                AtualizadoEm = DateTime.UtcNow,
            };
            db.Fornecedores.Add(f);
            await db.SaveChangesAsync();

            if (body.ProdutoIds?.Count > 0)
                await VincularProdutosAsync(db, f.Id, body.ProdutoIds);

            return Results.Ok(await MapearFornecedorAsync(db, f.Id));
        });

        g.MapPut("/fornecedores/{id:int}", async (int id, UpdateFornecedorRequest body, AppDbContext db) =>
        {
            var f = await db.Fornecedores.FindAsync(id);
            if (f == null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(body.NomeRazaoSocial))
                return Results.BadRequest("Nome/Razão Social é obrigatório.");
            if (string.IsNullOrWhiteSpace(body.WhatsApp))
                return Results.BadRequest("WhatsApp é obrigatório.");

            f.NomeRazaoSocial = body.NomeRazaoSocial.Trim();
            f.Email = string.IsNullOrWhiteSpace(body.Email) ? null : body.Email.Trim();
            f.WhatsApp = NormalizarWhatsApp(body.WhatsApp);
            f.Ativo = body.Ativo;
            f.AtualizadoEm = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(await MapearFornecedorAsync(db, f.Id));
        });

        g.MapDelete("/fornecedores/{id:int}", async (int id, AppDbContext db) =>
        {
            var f = await db.Fornecedores.FindAsync(id);
            if (f == null) return Results.NotFound();

            var temProdutos = await db.Produtos.AnyAsync(p => p.FornecedorId == id);
            if (temProdutos)
            {
                f.Ativo = false;
                f.AtualizadoEm = DateTime.UtcNow;
                await db.SaveChangesAsync();
                return Results.Ok(new { message = "Fornecedor desativado (possui produtos vinculados)." });
            }

            db.Fornecedores.Remove(f);
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Fornecedor excluído." });
        });

        g.MapGet("/fornecedores/{id:int}/produtos", async (int id, AppDbContext db) =>
        {
            if (!await db.Fornecedores.AnyAsync(f => f.Id == id))
                return Results.NotFound();

            var ids = await db.Produtos.AsNoTracking()
                .Where(p => p.FornecedorId == id)
                .Select(p => p.Id)
                .ToListAsync();
            return Results.Ok(new { produtoIds = ids });
        });

        g.MapPut("/fornecedores/{id:int}/produtos", async (int id, VincularProdutosFornecedorRequest body, AppDbContext db) =>
        {
            if (!await db.Fornecedores.AnyAsync(f => f.Id == id))
                return Results.NotFound();

            await VincularProdutosAsync(db, id, body.ProdutoIds ?? []);
            return Results.Ok(new { message = "Produtos vinculados com sucesso.", produtoIds = body.ProdutoIds });
        });

        // ─── Produtos ───
        g.MapGet("/produtos", async (AppDbContext db, string? loja, string? q, int? fornecedorId, bool? semFornecedor, int page = 1, int pageSize = 50) =>
        {
            if (page < 1) page = 1;
            if (pageSize is < 1 or > 100) pageSize = 50;

            var query = db.Produtos.AsNoTracking().Include(p => p.Fornecedor).AsQueryable();

            if (!string.IsNullOrWhiteSpace(loja))
            {
                var l = LojaOrigemHelper.Parse(loja);
                if (l.HasValue) query = query.Where(p => p.Loja == l.Value);
            }

            if (fornecedorId.HasValue)
                query = query.Where(p => p.FornecedorId == fornecedorId.Value);

            if (semFornecedor == true)
                query = query.Where(p => p.FornecedorId == null);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var t = q.Trim().ToLower();
                query = query.Where(p =>
                    p.Nome.ToLower().Contains(t) ||
                    (p.Sku != null && p.Sku.ToLower().Contains(t)) ||
                    (p.CodigoFornecedor != null && p.CodigoFornecedor.ToLower().Contains(t)) ||
                    p.ProdutoIdLojaIntegrada.ToString().Contains(t));
            }

            var total = await query.CountAsync();
            var rows = await query
                .OrderBy(p => p.Nome)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var items = rows.Select(MapearProduto).ToList();

            return Results.Ok(new PagedResultDto<ProdutoDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            });
        });

        g.MapGet("/produtos/{id:int}", async (int id, AppDbContext db) =>
        {
            var p = await db.Produtos.AsNoTracking().Include(x => x.Fornecedor).FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return Results.NotFound();
            return Results.Ok(MapearProduto(p));
        });

        g.MapPost("/produtos", async (CreateProdutoRequest body, AppDbContext db) =>
        {
            var loja = LojaOrigemHelper.Parse(body.Loja);
            if (!loja.HasValue)
                return Results.BadRequest("Loja inválida (use ResumeModas ou DonnaKora).");
            if (string.IsNullOrWhiteSpace(body.Nome))
                return Results.BadRequest("Nome é obrigatório.");
            if (body.ProdutoIdLojaIntegrada <= 0)
                return Results.BadRequest("ProdutoIdLojaIntegrada é obrigatório.");

            if (await db.Produtos.AnyAsync(p => p.Loja == loja && p.ProdutoIdLojaIntegrada == body.ProdutoIdLojaIntegrada))
                return Results.Conflict("Produto já cadastrado para esta loja.");

            var p = new Produto
            {
                Loja = loja.Value,
                ProdutoIdLojaIntegrada = body.ProdutoIdLojaIntegrada,
                Nome = body.Nome.Trim(),
                Sku = string.IsNullOrWhiteSpace(body.Sku) ? null : body.Sku.Trim(),
                CodigoFornecedor = string.IsNullOrWhiteSpace(body.CodigoFornecedor) ? null : body.CodigoFornecedor.Trim(),
                Ativo = body.Ativo,
                CriadoEm = DateTime.UtcNow,
                AtualizadoEm = DateTime.UtcNow,
            };
            db.Produtos.Add(p);
            await db.SaveChangesAsync();
            return Results.Ok(MapearProduto(p));
        });

        g.MapPut("/produtos/{id:int}", async (int id, UpdateProdutoRequest body, AppDbContext db) =>
        {
            var p = await db.Produtos.FindAsync(id);
            if (p == null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(body.Nome))
                return Results.BadRequest("Nome é obrigatório.");

            p.Nome = body.Nome.Trim();
            p.Sku = string.IsNullOrWhiteSpace(body.Sku) ? null : body.Sku.Trim();
            p.CodigoFornecedor = string.IsNullOrWhiteSpace(body.CodigoFornecedor) ? null : body.CodigoFornecedor.Trim();
            p.Ativo = body.Ativo;
            p.AtualizadoEm = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await db.Entry(p).Reference(x => x.Fornecedor).LoadAsync();
            return Results.Ok(MapearProduto(p));
        });

        g.MapDelete("/produtos/{id:int}", async (int id, AppDbContext db) =>
        {
            var p = await db.Produtos.FindAsync(id);
            if (p == null) return Results.NotFound();
            p.Ativo = false;
            p.AtualizadoEm = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Produto desativado." });
        });

        // ─── Notificações (Pedido de Compra) ───
        g.MapGet("/notificacoes/pedido-compra", async (
            AppDbContext db,
            DateTime? data,
            string? status,
            int? fornecedorId,
            string? pedido,
            int page = 1,
            int pageSize = 50) =>
        {
            if (page < 1) page = 1;
            if (pageSize is < 1 or > 100) pageSize = 50;

            var query = db.NotificacoesFornecedor.AsNoTracking()
                .Include(n => n.Fornecedor)
                .Include(n => n.Produto)
                .AsQueryable();

            if (data.HasValue)
            {
                var inicio = TimeZoneBrasil.DeBrasiliaParaUtc(data.Value.Date);
                var fim = TimeZoneBrasil.DeBrasiliaParaUtc(data.Value.Date.AddDays(1));
                query = query.Where(n => n.CriadoEm >= inicio && n.CriadoEm < fim);
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<StatusNotificacaoFornecedor>(status, true, out var st))
                query = query.Where(n => n.Status == st);

            if (fornecedorId.HasValue)
                query = query.Where(n => n.FornecedorId == fornecedorId.Value);

            if (!string.IsNullOrWhiteSpace(pedido))
            {
                var t = pedido.Trim().ToLower();
                query = query.Where(n => n.PedidoExternoId.ToLower().Contains(t));
            }

            var total = await query.CountAsync();
            var rows = await query
                .OrderByDescending(n => n.CriadoEm)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var items = rows.Select(MapearNotificacao).ToList();

            return Results.Ok(new PagedResultDto<NotificacaoFornecedorDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            });
        });

        g.MapGet("/notificacoes/pedido-compra/{id:int}", async (int id, AppDbContext db) =>
        {
            var n = await db.NotificacoesFornecedor.AsNoTracking()
                .Include(x => x.Fornecedor)
                .Include(x => x.Produto)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (n == null) return Results.NotFound();
            return Results.Ok(MapearNotificacao(n));
        });
    }

    private static async Task VincularProdutosAsync(AppDbContext db, int fornecedorId, List<int> produtoIds)
    {
        var ids = produtoIds.Distinct().ToList();
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var desvincular = await db.Produtos
                .Where(p => p.FornecedorId == fornecedorId && !ids.Contains(p.Id))
                .ToListAsync();
            foreach (var p in desvincular)
            {
                p.FornecedorId = null;
                p.AtualizadoEm = DateTime.UtcNow;
            }

            if (ids.Count > 0)
            {
                var vincular = await db.Produtos.Where(p => ids.Contains(p.Id)).ToListAsync();
                foreach (var p in vincular)
                {
                    p.FornecedorId = fornecedorId;
                    p.AtualizadoEm = DateTime.UtcNow;
                }
            }

            var fornecedor = await db.Fornecedores.FindAsync(fornecedorId);
            if (fornecedor != null)
                fornecedor.AtualizadoEm = DateTime.UtcNow;

            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static async Task<FornecedorDto> MapearFornecedorAsync(AppDbContext db, int id)
    {
        var f = await db.Fornecedores.AsNoTracking().FirstAsync(x => x.Id == id);
        var count = await db.Produtos.CountAsync(p => p.FornecedorId == id && p.Ativo);
        return new FornecedorDto
        {
            Id = f.Id,
            NomeRazaoSocial = f.NomeRazaoSocial,
            Email = f.Email,
            WhatsApp = f.WhatsApp,
            Ativo = f.Ativo,
            ProdutosVinculados = count,
            CriadoEm = f.CriadoEm,
            AtualizadoEm = f.AtualizadoEm,
        };
    }

    private static ProdutoDto MapearProduto(Produto p) => new()
    {
        Id = p.Id,
        Loja = LojaOrigemHelper.ToVendedorLabel(p.Loja),
        ProdutoIdLojaIntegrada = p.ProdutoIdLojaIntegrada,
        Nome = p.Nome,
        Sku = p.Sku,
        CodigoFornecedor = p.CodigoFornecedor,
        FornecedorId = p.FornecedorId,
        FornecedorNome = p.Fornecedor?.NomeRazaoSocial,
        Ativo = p.Ativo,
        CriadoEm = p.CriadoEm,
        AtualizadoEm = p.AtualizadoEm,
    };

    private static NotificacaoFornecedorDto MapearNotificacao(NotificacaoFornecedor n) => new()
    {
        Id = n.Id,
        PedidoId = n.PedidoId,
        PedidoItemId = n.PedidoItemId,
        FornecedorId = n.FornecedorId,
        FornecedorNome = n.Fornecedor?.NomeRazaoSocial,
        ProdutoId = n.ProdutoId,
        ProdutoNome = n.Produto?.Nome,
        Loja = LojaOrigemHelper.ToVendedorLabel(n.Loja),
        PedidoExternoId = n.PedidoExternoId,
        NomeCliente = n.NomeCliente,
        Status = n.Status.ToString(),
        MensagemTexto = n.MensagemTexto,
        WhatsAppMessageId = n.WhatsAppMessageId,
        Erro = n.Erro,
        CriadoEm = n.CriadoEm,
        EnviadoEm = n.EnviadoEm,
    };

    private static string NormalizarWhatsApp(string whatsapp)
    {
        var digits = new string(whatsapp.Where(char.IsDigit).ToArray());
        return digits;
    }
}
