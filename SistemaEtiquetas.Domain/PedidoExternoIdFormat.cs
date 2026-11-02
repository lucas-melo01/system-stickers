using System.Text.RegularExpressions;

namespace SistemaEtiquetas.Domain;

/// <summary>
/// Formatação de <c>PedidoExternoId</c> para exibição humana.
/// Pedidos Nuvemshop usam <c>ns:{storeId}:{number}</c> no banco; na UI/WhatsApp mostramos só o number.
/// </summary>
public static partial class PedidoExternoIdFormat
{
    public static string ParaExibicao(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return "";

        var trimmed = id.Trim();
        var m = NuvemshopPrefixRegex().Match(trimmed);
        return m.Success ? m.Groups[1].Value : trimmed;
    }

    [GeneratedRegex(@"^ns:\d+:(.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex NuvemshopPrefixRegex();
}
