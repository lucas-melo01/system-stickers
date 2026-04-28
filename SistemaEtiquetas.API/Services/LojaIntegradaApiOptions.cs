namespace SistemaEtiquetas.API.Services;

public sealed class LojaIntegradaApiOptions
{
    /// <summary>URL base da API (Loja Integrada).</summary>
    public string BaseUrl { get; set; } = "https://api.awsli.com.br";

    public LojaIntegradaLojaCredentials ResumeModas { get; set; } = new();

    public LojaIntegradaLojaCredentials DonnaKora { get; set; } = new();
}

public sealed class LojaIntegradaLojaCredentials
{
    public string? ChaveApi { get; set; }

    public string? Aplicacao { get; set; }
}
