namespace SistemaEtiquetas.API.Services;

public static class NuvemshopLojaMapper
{
    public static string ResolveAppKey(string? lojaOrStateOrVendedor)
    {
        if (string.IsNullOrWhiteSpace(lojaOrStateOrVendedor))
            return NuvemshopOptions.AppKeyResume;

        var key = lojaOrStateOrVendedor.Trim();
        if (key.Equals("donnakora", StringComparison.OrdinalIgnoreCase)
            || key.Equals("donna-kora", StringComparison.OrdinalIgnoreCase)
            || key.Equals("DonnaKora", StringComparison.OrdinalIgnoreCase)
            || key.Contains("donna", StringComparison.OrdinalIgnoreCase))
            return NuvemshopOptions.AppKeyDonna;

        return NuvemshopOptions.AppKeyResume;
    }

    public static string ResolveVendedor(string? lojaOrState)
    {
        return ResolveAppKey(lojaOrState) == NuvemshopOptions.AppKeyDonna
            ? "DonnaKora"
            : "Resume Modas";
    }

    public static string WebhookPathPrefix(string appKey) =>
        ResolveAppKey(appKey) == NuvemshopOptions.AppKeyDonna
            ? "/webhook/nuvemshop/donnakora"
            : "/webhook/nuvemshop";
}
