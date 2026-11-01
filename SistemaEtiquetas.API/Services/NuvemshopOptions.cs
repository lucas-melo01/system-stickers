namespace SistemaEtiquetas.API.Services;

public sealed class NuvemshopOptions
{
    public string AppId { get; set; } = "";

    public string ClientSecret { get; set; } = "";

    public string UserAgent { get; set; } = "SistemaEtiquetas (contato@exemplo.com)";

    public string ApiBaseUrl { get; set; } = "https://api.nuvemshop.com.br/2025-03";

    public string AuthBaseUrl { get; set; } = "https://www.nuvemshop.com.br";

    /// <summary>URL pública HTTPS da API (ex.: https://api.seudominio.com) para registrar webhooks e OAuth.</summary>
    public string PublicBaseUrl { get; set; } = "";
}
