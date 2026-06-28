using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.API.DTO;
using SistemaEtiquetas.API.Extensions;
using SistemaEtiquetas.API.Services;
using SistemaEtiquetas.Domain;
using SistemaEtiquetas.Domain.Entities;
using SistemaEtiquetas.Infrastructure.Data;
using System.Text.Json;

namespace SistemaEtiquetas.API.Endpoints;

public static class WebhookPedidoHandler
{
    public static async Task<IResult> Processar(
        string payload,
        string vendedor,
        AppDbContext db,
        LojaIntegradaProdutoApi catalogoProduto,
        EncomendaNotificacaoService? encomendaNotificacao = null)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var pedidoDto = JsonSerializer.Deserialize<WebhookPedidoDto>(payload, options);
        if (pedidoDto?.situacao?.codigo != "pedido_pago")
            return Results.Ok("Pedido não processado: status diferente de 'pedido_pago'");

        var pedidoExternoId = !string.IsNullOrWhiteSpace(pedidoDto.numero)
            ? pedidoDto.numero.Trim()
            : pedidoDto.id.ToString();

        if (await db.Pedidos.AnyAsync(p => p.PedidoExternoId == pedidoExternoId))
            return Results.Ok("Pedido já processado");

        var dataPedido = TimeZoneBrasil.ParaUtcConsiderandoBrasilia(pedidoDto.data_criacao);
        var tipoEnvio = pedidoDto.envios?.FirstOrDefault()?.forma_envio?.nome ?? "N/A";
        var valorFrete = pedidoDto.envios?.FirstOrDefault()?.valor
            ?? pedidoDto.valor_envio
            ?? 0m;
        var formaPagamento = pedidoDto.pagamentos?.FirstOrDefault()?.forma_pagamento?.nome ?? "N/A";
        var pedido = new Pedido
        {
            PedidoExternoId = pedidoExternoId,
            NomeCliente = pedidoDto.cliente?.nome,
            DataPedido = dataPedido,
            ClienteCpf = pedidoDto.cliente?.cpf,
            Vendedor = vendedor,
            TipoEnvio = tipoEnvio,
            FormaPagamento = formaPagamento,
            ValorFrete = valorFrete,
            jsonWebhook = payload
        };

        var itensEncomenda = new List<(PedidoItem item, long produtoIdLojaIntegrada)>();

        foreach (var item in pedidoDto.itens)
        {
            var skuOriginal = item.sku;
            var ehEncomenda = EhEncomenda(skuOriginal);
            var skuParaParse = ehEncomenda ? RemoverSufixoEnc(skuOriginal) : skuOriginal;

            var (skuLegado, cor, tamanho) = ParsearSku(skuParaParse);
            string sku;
            if (item.produto_id > 0)
            {
                var mpn = await catalogoProduto.ObterMpnAsync(item.produto_id, vendedor);
                sku = string.IsNullOrWhiteSpace(mpn) ? string.Empty : mpn.Trim();
            }
            else
            {
                sku = skuLegado;
            }

            var n = item.quantidade > 0 ? item.quantidade : 1;
            var valorVenda = item.preco_venda ?? 0m;
            var valorCusto = item.preco_custo ?? 0m;
            for (var i = 0; i < n; i++)
            {
                var pedidoItem = new PedidoItem
                {
                    Produto = item.nome,
                    SKU = sku,
                    Cor = cor,
                    Tamanho = tamanho,
                    Quantidade = 1,
                    ValorCusto = i == 0 ? valorCusto : 0,
                    ValorVenda = i == 0 ? valorVenda : 0
                };
                pedido.Itens.Add(pedidoItem);

                if (ehEncomenda && item.produto_id > 0)
                    itensEncomenda.Add((pedidoItem, item.produto_id));
            }
        }

        db.Pedidos.Add(pedido);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbDuplicateKeyHelper.IsUniqueViolation(ex))
        {
            return Results.Ok("Pedido já processado");
        }

        if (itensEncomenda.Count > 0 && encomendaNotificacao != null)
        {
            var loja = LojaOrigemHelper.FromVendedor(vendedor);
            if (loja.HasValue)
            {
                try
                {
                    await encomendaNotificacao.ProcessarEncomendasAsync(
                        pedido, loja.Value, vendedor, itensEncomenda);
                }
                catch (Exception ex)
                {
                    // Pedido já salvo; falha na notificação não deve reverter o pedido
                    Console.Error.WriteLine($"Erro ao processar notificações encomenda: {ex.Message}");
                }
            }
        }

        return Results.Ok("Pedido salvo com sucesso");
    }

    public static bool EhEncomenda(string? sku) =>
        !string.IsNullOrWhiteSpace(sku) &&
        sku.TrimEnd().EndsWith("-enc", StringComparison.OrdinalIgnoreCase);

    public static string? RemoverSufixoEnc(string? sku)
    {
        if (string.IsNullOrWhiteSpace(sku)) return sku;
        var s = sku.TrimEnd();
        if (s.EndsWith("-enc", StringComparison.OrdinalIgnoreCase))
            return s[..^4];
        return s;
    }

    public static (string sku, string? cor, string? tamanho) ParsearSku(string? skuCompleto)
    {
        if (string.IsNullOrWhiteSpace(skuCompleto))
            return (skuCompleto ?? "", null, null);
        var partes = skuCompleto.Split('-');
        if (partes.Length < 3)
            return (skuCompleto, null, null);
        var sku = partes[0];
        var cor = partes[1];
        var tamanho = string.Join("-", partes.Skip(2));
        return (sku, cor, tamanho);
    }
}
