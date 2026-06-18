namespace SistemaEtiquetas.Domain;

public static class PedidoExternoIdGenerator
{
    /// <summary>
    /// Gera o próximo ID externo numérico com base no maior valor entre pedidos manuais.
    /// </summary>
    public static string GerarProximo(IEnumerable<string> idsManuais)
    {
        long max = 0;
        foreach (var id in idsManuais)
        {
            if (!string.IsNullOrWhiteSpace(id) && long.TryParse(id.Trim(), out var n) && n > max)
                max = n;
        }
        return (max + 1).ToString();
    }

    /// <summary>
    /// Gera o próximo ID a partir da sequência manual, pulando IDs já usados (ex.: webhook).
    /// </summary>
    public static string GerarProximoDisponivel(IEnumerable<string> idsManuais, IEnumerable<string> idsOcupados)
    {
        if (!long.TryParse(GerarProximo(idsManuais), out var candidato))
            candidato = 1;

        var ocupados = idsOcupados
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .ToHashSet(StringComparer.Ordinal);

        while (ocupados.Contains(candidato.ToString()))
            candidato++;

        return candidato.ToString();
    }
}
