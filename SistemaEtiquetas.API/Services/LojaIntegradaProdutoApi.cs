using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SistemaEtiquetas.API.Services;

/// <summary>
/// Loja Integrada: <c>GET {base}/v1/produto/{produtoId}</c> com
/// <c>Authorization: chave_api {key} aplicacao {guid}</c>.
/// </summary>
public sealed class LojaIntegradaProdutoApi
{
    private const string CdnBase = "https://cdn.awsli.com.br/";

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

    public sealed class ProdutoDetalheLi
    {
        public string? Nome { get; init; }
        public string? Mpn { get; init; }
        public string? ImagemUrl { get; init; }
        public string? Pai { get; init; }
    }

    /// <summary>Devolve o MPN ou null se credenciais ausentes, erro HTTP ou resposta sem mpn.</summary>
    public async Task<string?> ObterMpnAsync(
        long produtoId,
        string vendedor,
        CancellationToken cancellationToken = default)
    {
        var detalhes = await ObterDetalhesProdutoAsync(produtoId, vendedor, cancellationToken);
        return detalhes?.Mpn;
    }

    /// <summary>Nome + MPN + URL pública da imagem (CDN awsli), sem persistir.</summary>
    public async Task<ProdutoDetalheLi?> ObterDetalhesProdutoAsync(
        long produtoId,
        string vendedor,
        CancellationToken cancellationToken = default)
    {
        var dto = await FetchProdutoAsync(produtoId, vendedor, cancellationToken);
        if (dto == null) return null;

        var nome = dto.nome?.Trim();
        var mpn = dto.mpn?.Trim();
        var imagemUrl = ResolverImagemUrl(dto);
        return new ProdutoDetalheLi
        {
            Nome = string.IsNullOrEmpty(nome) ? null : nome,
            Mpn = string.IsNullOrEmpty(mpn) ? null : mpn,
            ImagemUrl = imagemUrl,
            Pai = dto.pai?.Trim(),
        };
    }

    /// <summary>
    /// Resolve o nome do produto: se o produto for uma variação (nome vazio, pai preenchido),
    /// busca o nome no produto pai. Retorna null se nada for encontrado.
    /// </summary>
    public async Task<string?> ObterNomeAsync(
        long produtoId,
        string vendedor,
        string? paiUri = null,
        CancellationToken cancellationToken = default)
    {
        var detalhes = await ObterDetalhesProdutoAsync(produtoId, vendedor, cancellationToken);
        if (detalhes == null) return null;

        if (!string.IsNullOrEmpty(detalhes.Nome))
            return detalhes.Nome;

        // Variação: tenta obter nome do produto pai
        var paiUriResolvido = detalhes.Pai ?? paiUri;
        if (string.IsNullOrEmpty(paiUriResolvido)) return null;

        var paiIdStr = paiUriResolvido.TrimEnd('/').Split('/').Last();
        if (!long.TryParse(paiIdStr, out var paiId) || paiId <= 0) return null;

        var paiDto = await FetchProdutoAsync(paiId, vendedor, cancellationToken);
        return paiDto?.nome?.Trim() is { Length: > 0 } n ? n : null;
    }

    private async Task<ProdutoV1Response?> FetchProdutoAsync(
        long produtoId,
        string vendedor,
        CancellationToken cancellationToken)
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
            return await JsonSerializer.DeserializeAsync<ProdutoV1Response>(stream, JsonOpts, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao obter produto {ProdutoId} ({Vendedor})", produtoId, vendedor);
            return null;
        }
    }

    private static string? ResolverImagemUrl(ProdutoV1Response dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.imagem_url))
            return dto.imagem_url.Trim();

        if (!string.IsNullOrWhiteSpace(dto.imagem))
            return MontarCdnUrl(dto.imagem);

        if (dto.imagens != null)
        {
            foreach (var img in dto.imagens)
            {
                if (!string.IsNullOrWhiteSpace(img.imagem_url))
                    return img.imagem_url.Trim();
                if (!string.IsNullOrWhiteSpace(img.caminho))
                    return MontarCdnUrl(img.caminho);
                if (!string.IsNullOrWhiteSpace(img.imagem))
                    return MontarCdnUrl(img.imagem);
            }
        }

        return null;
    }

    private static string MontarCdnUrl(string path)
    {
        var p = path.Trim().TrimStart('/');
        if (p.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            p.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return p;
        return CdnBase + p;
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
        public string? nome { get; set; }
        public string? mpn { get; set; }
        public string? pai { get; set; }
        public string? imagem { get; set; }
        public string? imagem_url { get; set; }
        public List<ProdutoImagemResponse>? imagens { get; set; }
    }

    private sealed class ProdutoImagemResponse
    {
        public string? imagem { get; set; }
        public string? imagem_url { get; set; }
        public string? caminho { get; set; }
    }
}
