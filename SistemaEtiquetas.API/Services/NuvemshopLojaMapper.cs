namespace SistemaEtiquetas.API.Services;

public static class NuvemshopLojaMapper
{
    public static string ResolveVendedor(string? lojaOrState)
    {
        if (string.IsNullOrWhiteSpace(lojaOrState))
            return "Resume Modas";

        var key = lojaOrState.Trim();
        if (key.Equals("donnakora", StringComparison.OrdinalIgnoreCase)
            || key.Equals("donna-kora", StringComparison.OrdinalIgnoreCase)
            || key.Equals("DonnaKora", StringComparison.OrdinalIgnoreCase))
            return "DonnaKora";

        if (key.Equals("resumemodas", StringComparison.OrdinalIgnoreCase)
            || key.Equals("resume-modas", StringComparison.OrdinalIgnoreCase)
            || key.Equals("resume", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Resume Modas", StringComparison.OrdinalIgnoreCase))
            return "Resume Modas";

        // state pode ser "loja=donnakora" ou similar
        if (key.Contains("donna", StringComparison.OrdinalIgnoreCase))
            return "DonnaKora";

        return "Resume Modas";
    }
}
