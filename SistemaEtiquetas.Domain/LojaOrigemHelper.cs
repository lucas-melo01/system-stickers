using SistemaEtiquetas.Domain.Entities;

namespace SistemaEtiquetas.Domain;

public static class LojaOrigemHelper
{
    public static LojaOrigem? FromVendedor(string? vendedor)
    {
        if (string.Equals(vendedor, "Resume Modas", StringComparison.OrdinalIgnoreCase))
            return LojaOrigem.ResumeModas;
        if (string.Equals(vendedor, "DonnaKora", StringComparison.OrdinalIgnoreCase))
            return LojaOrigem.DonnaKora;
        return null;
    }

    public static string ToVendedorLabel(LojaOrigem loja) => loja switch
    {
        LojaOrigem.ResumeModas => "Resume Modas",
        LojaOrigem.DonnaKora => "DonnaKora",
        _ => loja.ToString(),
    };

    public static LojaOrigem? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (Enum.TryParse<LojaOrigem>(value, true, out var e)) return e;
        if (value.Equals("Resume Modas", StringComparison.OrdinalIgnoreCase)) return LojaOrigem.ResumeModas;
        if (value.Equals("ResumeModas", StringComparison.OrdinalIgnoreCase)) return LojaOrigem.ResumeModas;
        if (value.Equals("DonnaKora", StringComparison.OrdinalIgnoreCase)) return LojaOrigem.DonnaKora;
        return null;
    }
}
