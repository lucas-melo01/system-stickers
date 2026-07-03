using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.API.DTO;
using SistemaEtiquetas.API.Services;
using SistemaEtiquetas.Domain;
using SistemaEtiquetas.Domain.Entities;
using SistemaEtiquetas.Infrastructure.Data;
using System.Text.Json;

namespace SistemaEtiquetas.API.Endpoints;

public static class WebhookProdutoHandler
{
    public static async Task<IResult> Processar(
        string payload,
        LojaOrigem loja,
        AppDbContext db,
        LojaIntegradaProdutoApi catalogo)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dto = JsonSerializer.Deserialize<WebhookProdutoDto>(payload, options);
        if (dto == null || dto.id <= 0)
            return Results.BadRequest("Payload de produto inválido.");

        var existente = await db.Produtos
            .FirstOrDefaultAsync(p => p.Loja == loja && p.ProdutoIdLojaIntegrada == dto.id);

        if (dto.removido)
        {
            if (existente != null)
            {
                existente.Ativo = false;
                existente.AtualizadoEm = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
            return Results.Ok("Produto desativado (removido na LI).");
        }

        // Variações (atributo_opcao) chegam com nome vazio — busca o nome no produto pai
        var nome = (dto.nome ?? "").Trim();
        if (string.IsNullOrEmpty(nome))
        {
            var vendedor = LojaOrigemHelper.ToVendedorLabel(loja);
            nome = await catalogo.ObterNomeAsync(dto.id, vendedor, dto.pai) ?? "";
        }

        if (string.IsNullOrEmpty(nome))
            return Results.Ok("Produto ignorado: variação sem nome e sem produto pai acessível.");

        var sku = string.IsNullOrWhiteSpace(dto.sku) ? null : dto.sku.Trim();
        var mpn = string.IsNullOrWhiteSpace(dto.mpn) ? null : dto.mpn.Trim();

        if (existente == null)
        {
            db.Produtos.Add(new Produto
            {
                Loja = loja,
                ProdutoIdLojaIntegrada = dto.id,
                Nome = nome,
                Sku = sku,
                CodigoFornecedor = mpn,
                Ativo = dto.ativo,
                FornecedorId = null,
                CriadoEm = DateTime.UtcNow,
                AtualizadoEm = DateTime.UtcNow,
            });
        }
        else
        {
            existente.Nome = nome;
            existente.Sku = sku;
            existente.CodigoFornecedor = mpn;
            existente.Ativo = dto.ativo;
            existente.AtualizadoEm = DateTime.UtcNow;
            // FornecedorId nunca alterado pelo webhook
        }

        await db.SaveChangesAsync();
        return Results.Ok("Produto sincronizado com sucesso.");
    }
}
