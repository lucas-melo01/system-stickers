using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SistemaEtiquetas.API.Services;

/// <summary>
/// Loja Integrada: <c>GET {base}/v1/produto/{produtoId}</c> com
/// <c>Authorization: chave_api {key} aplicacao {guid}</c>.
/// </summary>
public sealed class LojaIntegradaProdutoApi
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly LojaIntegradaApiOptions _opt;
    private readonly ILogger<LojaIntegradaProdutoApi> _logger;

    public LojaIntegradaProdutoApi(
        HttpClient http,
        IOptions<LojaIntegradaApiOptions> opt,
        ILogger<LojaIntegradaProdutoApi> logger)
    {
        _http = http;
        _opt = opt.Value;
        _logger = logger;
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>Devolve o MPN ou null se credenciais ausentes, erro HTTP ou resposta sem mpn.</summary>
    public async Task<string?> ObterMpnAsync(
        long produtoId,
        string vendedor,
        CancellationToken cancellationToken = default)
    {
        var creds = ResolverCredenciais(vendedor);
        var baseUrl = (_opt.BaseUrl ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl) || produtoId <= 0 || creds == null)
            return null;

        var chave = (creds.ChaveApi ?? "").Trim();
        var app = (creds.Aplicacao ?? "").Trim();
        if (chave.Length == 0 || app.Length == 0)
            return null;

        var url = $"{baseUrl}/v1/produto/{produtoId}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        var authValue = $"chave_api {chave} aplicacao {app}";
        req.Headers.TryAddWithoutValidation("Authorization", authValue);

        try
        {
            var response = await _http.SendAsync(req, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Loja Integrada produto {ProdutoId} ({Vendedor}): HTTP {Status}",
                    produtoId,
                    vendedor,
                    (int)response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var dto = await JsonSerializer.DeserializeAsync<ProdutoV1Response>(stream, JsonOpts, cancellationToken);
            var mpn = dto?.mpn?.Trim();
            return string.IsNullOrEmpty(mpn) ? null : mpn;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao obter MPN do produto {ProdutoId} ({Vendedor})", produtoId, vendedor);
            return null;
        }
    }

    private LojaIntegradaLojaCredentials? ResolverCredenciais(string vendedor)
    {
        if (string.Equals(vendedor, "Resume Modas", StringComparison.OrdinalIgnoreCase))
            return _opt.ResumeModas;
        if (string.Equals(vendedor, "DonnaKora", StringComparison.OrdinalIgnoreCase))
            return _opt.DonnaKora;
        return null;
    }

    private sealed class ProdutoV1Response
    {
        public string? mpn { get; set; }
    }
}
