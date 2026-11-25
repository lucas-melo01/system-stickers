namespace SistemaEtiquetas.API.Services;

public sealed class NuvemshopOptions
{
    /// <summary>Legado: se ResumeModas.AppId estiver vazio, usa este valor.</summary>
    public string AppId { get; set; } = "";

    /// <summary>Legado: se ResumeModas.ClientSecret estiver vazio, usa este valor.</summary>
    public string ClientSecret { get; set; } = "";

    public string UserAgent { get; set; } = "SistemaEtiquetas (contato@exemplo.com)";

    public string ApiBaseUrl { get; set; } = "https://api.nuvemshop.com.br/2025-03";

    public string AuthBaseUrl { get; set; } = "https://www.nuvemshop.com.br";

    /// <summary>URL pública HTTPS da API (ex.: https://api.seudominio.com) para registrar webhooks e OAuth.</summary>
    public string PublicBaseUrl { get; set; } = "";

    public NuvemshopAppCredentials ResumeModas { get; set; } = new();

    public NuvemshopAppCredentials DonnaKora { get; set; } = new()
    {
        StoreId = 8277644,
    };

    public const string AppKeyResume = "ResumeModas";
    public const string AppKeyDonna = "DonnaKora";

    public NuvemshopAppCredentials GetCredentials(string appKey)
    {
        NormalizeLegacy();
        if (string.Equals(appKey, AppKeyDonna, StringComparison.OrdinalIgnoreCase))
            return DonnaKora;
        return ResumeModas;
    }

    public void NormalizeLegacy()
    {
        if (string.IsNullOrWhiteSpace(ResumeModas.AppId) && !string.IsNullOrWhiteSpace(AppId))
            ResumeModas.AppId = AppId;
        if (string.IsNullOrWhiteSpace(ResumeModas.ClientSecret) && !string.IsNullOrWhiteSpace(ClientSecret))
            ResumeModas.ClientSecret = ClientSecret;
        if (DonnaKora.StoreId is null or <= 0)
            DonnaKora.StoreId = 8277644;
    }
}

public sealed class NuvemshopAppCredentials
{
    public string AppId { get; set; } = "";

    public string ClientSecret { get; set; } = "";

    /// <summary>StoreId esperado no OAuth (ex.: 8277644 na DonnaKora). Null/0 = não valida.</summary>
    public long? StoreId { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AppId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
