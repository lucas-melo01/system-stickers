using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.API.DTO;
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
        ILogger? logger = null)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var pedidoDto = JsonSerializer.Deserialize<WebhookPedidoDto>(payload, options);
        if (pedidoDto?.situacao?.codigo != "pedido_pago")
            return Results.Ok("Pedido não processado: status diferente de 'pedido_pago'");

        if (await db.Pedidos.AnyAsync(p => p.PedidoExternoId == pedidoDto.id.ToString()))
            return Results.Ok("Pedido já processado");

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
            Vendedor = vendedor,
            TipoEnvio = tipoEnvio,
            FormaPagamento = formaPagamento,
            ValorFrete = valorFrete,
            jsonWebhook = payload
        };
        foreach (var item in pedidoDto.itens)
        {
            var (sku, cor, tamanho) = ParsearSku(item.sku);
            pedido.Itens.Add(new PedidoItem
            {
                Produto = item.nome,
                SKU = sku,
                Cor = cor,
                Tamanho = tamanho,
                Quantidade = item.quantidade
            });
        }
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync();
        return Results.Ok("Pedido salvo com sucesso");
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
