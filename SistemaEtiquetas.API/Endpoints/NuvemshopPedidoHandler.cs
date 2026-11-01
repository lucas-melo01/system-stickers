using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.API.DTO;
using SistemaEtiquetas.API.Extensions;
using SistemaEtiquetas.API.Services;
using SistemaEtiquetas.Domain.Entities;
using SistemaEtiquetas.Infrastructure.Data;
using System.Text.Json;

namespace SistemaEtiquetas.API.Endpoints;

public static class NuvemshopPedidoHandler
{
    public static async Task<IResult> ProcessarOrderPaidAsync(
        long storeId,
        long orderId,
        AppDbContext db,
        NuvemshopApiClient api,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var instalacao = await db.NuvemshopInstalacoes
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.StoreId == storeId && i.Ativo, cancellationToken);

        if (instalacao == null)
        {
            logger.LogWarning("Nuvemshop order/paid sem instalação ativa store={StoreId}", storeId);
            return Results.Ok("Instalação não encontrada");
        }

        var (order, rawJson) = await api.GetOrderAsync(
            storeId, orderId, instalacao.AccessToken, cancellationToken);

        if (order == null || string.IsNullOrEmpty(rawJson))
            return Results.Ok("Pedido não obtido da API Nuvemshop");

        if (!string.Equals(order.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
            return Results.Ok($"Pedido não processado: payment_status={order.PaymentStatus}");

        var pedidoExternoId = $"ns:{storeId}:{order.Number}";
        if (await db.Pedidos.AnyAsync(p => p.PedidoExternoId == pedidoExternoId, cancellationToken))
            return Results.Ok("Pedido já processado");

        var dataPedido = ResolverDataPedido(order);
        var formaPagamento = !string.IsNullOrWhiteSpace(order.GatewayName)
            ? order.GatewayName!.Trim()
            : (order.PaymentDetails?.Method?.Trim() ?? "N/A");
        var (tipoEnvio, valorFrete) = ResolverEnvio(order);

        var pedido = new Pedido
        {
            PedidoExternoId = pedidoExternoId,
            NomeCliente = string.IsNullOrWhiteSpace(order.ContactName) ? "Cliente" : order.ContactName.Trim(),
            ClienteCpf = string.IsNullOrWhiteSpace(order.ContactIdentification)
                ? null
                : order.ContactIdentification.Trim(),
            DataPedido = dataPedido,
            Vendedor = instalacao.Vendedor,
            TipoEnvio = tipoEnvio,
            FormaPagamento = formaPagamento,
            ValorFrete = valorFrete,
            jsonWebhook = rawJson,
        };

        foreach (var item in order.Products ?? [])
        {
            var qty = Math.Max(1, NuvemshopJsonHelpers.ParseInt(item.Quantity, 1));
            var valorVenda = NuvemshopJsonHelpers.ParseDecimal(item.Price);
            var skuRaw = item.Sku?.Trim() ?? "";
            var (cor, tamanho) = ResolverCorTamanho(item);
            var sku = skuRaw;
            if (string.IsNullOrEmpty(cor) && string.IsNullOrEmpty(tamanho) && skuRaw.Contains('-'))
            {
                var parsed = WebhookPedidoHandler.ParsearSku(skuRaw);
                if (!string.IsNullOrEmpty(parsed.cor) || !string.IsNullOrEmpty(parsed.tamanho))
                {
                    sku = parsed.sku;
                    cor = parsed.cor;
                    tamanho = parsed.tamanho;
                }
            }

            for (var i = 0; i < qty; i++)
            {
                pedido.Itens.Add(new PedidoItem
                {
                    Produto = string.IsNullOrWhiteSpace(item.Name) ? "Produto" : item.Name.Trim(),
                    SKU = sku,
                    Cor = cor,
                    Tamanho = tamanho,
                    Quantidade = 1,
                    ValorVenda = i == 0 ? valorVenda : 0,
                    ValorCusto = 0,
                });
            }
        }

        if (pedido.Itens.Count == 0)
            return Results.Ok("Pedido sem itens");

        db.Pedidos.Add(pedido);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (DbDuplicateKeyHelper.IsUniqueViolation(ex))
        {
            return Results.Ok("Pedido já processado");
        }

        return Results.Ok("Pedido salvo com sucesso");
    }

    public static async Task<IResult> ProcessarAppUninstalledAsync(
        long storeId,
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var instalacao = await db.NuvemshopInstalacoes
            .FirstOrDefaultAsync(i => i.StoreId == storeId, cancellationToken);
        if (instalacao == null)
            return Results.Ok("Instalação não encontrada");

        instalacao.Ativo = false;
        instalacao.AccessToken = "";
        instalacao.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Nuvemshop app desinstalado store={StoreId}", storeId);
        return Results.Ok("Instalação desativada");
    }

    private static DateTime ResolverDataPedido(NuvemshopOrderDto order)
    {
        var raw = order.PaidAt ?? order.CreatedAt ?? DateTime.UtcNow;
        if (raw.Kind == DateTimeKind.Unspecified)
        {
            // ISO da Nuvemshop costuma vir com offset; se Unspecified, tratar como UTC
            return DateTime.SpecifyKind(raw, DateTimeKind.Utc);
        }

        return raw.ToUniversalTime();
    }

    private static (string tipoEnvio, decimal valorFrete) ResolverEnvio(NuvemshopOrderDto order)
    {
        var fulfillment = order.Fulfillments?.FirstOrDefault();
        if (fulfillment == null)
            return ("N/A", 0m);

        var nome = fulfillment.ShippingOption
            ?? fulfillment.ShippingMethod?.Name
            ?? fulfillment.Shipping?.Option
            ?? fulfillment.Shipping?.Carrier?.Name
            ?? fulfillment.Shipping?.Type;

        var frete = 0m;
        if (fulfillment.ShippingCost.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
            frete = NuvemshopJsonHelpers.ParseDecimal(fulfillment.ShippingCost);
        else if (fulfillment.Shipping?.Cost.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
            frete = NuvemshopJsonHelpers.ParseDecimal(fulfillment.Shipping.Cost);

        return (string.IsNullOrWhiteSpace(nome) ? "N/A" : nome.Trim(), frete);
    }

    private static (string? cor, string? tamanho) ResolverCorTamanho(NuvemshopOrderProductDto item)
    {
        var values = (item.VariantValues ?? [])
            .Select(NuvemshopJsonHelpers.VariantValueAsString)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!.Trim())
            .ToList();

        if (values.Count >= 2)
            return (values[0], values[1]);
        if (values.Count == 1)
            return (values[0], null);

        return (null, null);
    }
}
