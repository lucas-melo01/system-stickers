using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.Infrastructure.Data;

var dryRun = args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (string.IsNullOrWhiteSpace(databaseUrl))
{
    Console.Error.WriteLine("Defina a variável DATABASE_URL com a connection string do Postgres.");
    return 1;
}

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(databaseUrl)
    .Options;

await using var db = new AppDbContext(options);

var pedidos = await db.Pedidos
    .Include(p => p.Itens)
    .Where(p => p.jsonWebhook != null && p.jsonWebhook != "")
    .OrderBy(p => p.Id)
    .ToListAsync();

Console.WriteLine($"Pedidos com jsonWebhook: {pedidos.Count}");
if (dryRun)
    Console.WriteLine("Modo --dry-run: nenhuma alteração será gravada.");

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

var pedidosAtualizados = 0;
var itensAtualizados = 0;
var itensIgnorados = 0;
var avisos = 0;

foreach (var pedido in pedidos)
{
    List<WebhookItemPreco>? itensWebhook;
    try
    {
        itensWebhook = ExtrairItensComPreco(pedido.jsonWebhook!, jsonOptions);
    }
    catch (Exception ex)
    {
        avisos++;
        Console.WriteLine($"[AVISO] Pedido {pedido.Id} ({pedido.PedidoExternoId}): JSON inválido — {ex.Message}");
        continue;
    }

    if (itensWebhook.Count == 0)
        continue;

    var itensDb = pedido.Itens.OrderBy(i => i.Id).ToList();
    var idsUsados = new HashSet<int>();
    var alterouPedido = false;

    foreach (var itemWebhook in itensWebhook)
    {
        if (string.IsNullOrWhiteSpace(itemWebhook.Nome))
        {
            avisos++;
            Console.WriteLine($"[AVISO] Pedido {pedido.Id}: item do webhook sem nome, ignorado.");
            continue;
        }

        if (!itemWebhook.PrecoVenda.HasValue)
        {
            itensIgnorados++;
            continue;
        }

        var qtd = itemWebhook.Quantidade > 0 ? itemWebhook.Quantidade : 1;
        var nome = itemWebhook.Nome.Trim();
        var linhas = itensDb
            .Where(i => !idsUsados.Contains(i.Id)
                && string.Equals(i.Produto.Trim(), nome, StringComparison.Ordinal))
            .Take(qtd)
            .ToList();

        if (linhas.Count == 0)
        {
            avisos++;
            Console.WriteLine(
                $"[AVISO] Pedido {pedido.Id} ({pedido.PedidoExternoId}): nenhum PedidoItem com Produto \"{nome}\".");
            continue;
        }

        if (linhas.Count < qtd)
        {
            avisos++;
            Console.WriteLine(
                $"[AVISO] Pedido {pedido.Id} ({pedido.PedidoExternoId}): produto \"{nome}\" — " +
                $"esperadas {qtd} linha(s), encontradas {linhas.Count}.");
        }

        for (var i = 0; i < linhas.Count; i++)
        {
            var valor = i == 0 ? itemWebhook.PrecoVenda.Value : 0m;
            var linha = linhas[i];

            if (linha.ValorVenda == valor)
            {
                idsUsados.Add(linha.Id);
                continue;
            }

            Console.WriteLine(
                $"  Pedido {pedido.Id} | Item {linha.Id} | {linha.Produto} | ValorVenda {linha.ValorVenda} -> {valor}");

            if (!dryRun)
                linha.ValorVenda = valor;

            idsUsados.Add(linha.Id);
            itensAtualizados++;
            alterouPedido = true;
        }
    }

    if (alterouPedido)
        pedidosAtualizados++;
}

if (!dryRun && itensAtualizados > 0)
    await db.SaveChangesAsync();

Console.WriteLine();
Console.WriteLine($"Concluído. Pedidos alterados: {pedidosAtualizados}. Itens atualizados: {itensAtualizados}.");
Console.WriteLine($"Itens sem preco_venda no webhook: {itensIgnorados}. Avisos: {avisos}.");

return 0;

static List<WebhookItemPreco> ExtrairItensComPreco(string json, JsonSerializerOptions options)
{
    using var doc = JsonDocument.Parse(json);
    if (!doc.RootElement.TryGetProperty("itens", out var itensEl) || itensEl.ValueKind != JsonValueKind.Array)
        return [];

    var resultado = new List<WebhookItemPreco>();

    foreach (var itemEl in itensEl.EnumerateArray())
    {
        var nome = itemEl.TryGetProperty("nome", out var nomeEl) && nomeEl.ValueKind == JsonValueKind.String
            ? nomeEl.GetString()
            : null;

        decimal? precoVenda = null;
        if (itemEl.TryGetProperty("preco_venda", out var precoEl))
            precoVenda = LerDecimal(precoEl);

        var quantidade = 1;
        if (itemEl.TryGetProperty("quantidade", out var qtdEl) && qtdEl.ValueKind == JsonValueKind.Number)
        {
            if (qtdEl.TryGetInt32(out var qtdInt))
                quantidade = qtdInt;
        }

        resultado.Add(new WebhookItemPreco(nome, precoVenda, quantidade));
    }

    return resultado;
}

static decimal? LerDecimal(JsonElement el)
{
    return el.ValueKind switch
    {
        JsonValueKind.Number => el.TryGetDecimal(out var d) ? d : null,
        JsonValueKind.String => decimal.TryParse(
            el.GetString(),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : decimal.TryParse(el.GetString(), out parsed) ? parsed : null,
        JsonValueKind.Null => null,
        _ => null,
    };
}

file sealed record WebhookItemPreco(string? Nome, decimal? PrecoVenda, int Quantidade);
