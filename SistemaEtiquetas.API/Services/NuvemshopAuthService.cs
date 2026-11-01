using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SistemaEtiquetas.API.DTO;

namespace SistemaEtiquetas.API.Services;

public sealed class NuvemshopAuthService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly NuvemshopOptions _opt;
    private readonly ILogger<NuvemshopAuthService> _logger;

    public NuvemshopAuthService(
        HttpClient http,
        IOptions<NuvemshopOptions> opt,
        ILogger<NuvemshopAuthService> logger)
    {
        _http = http;
        _opt = opt.Value;
        _logger = logger;
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public string BuildAuthorizeUrl(string state)
    {
        var baseUrl = _opt.AuthBaseUrl.TrimEnd('/');
        return $"{baseUrl}/apps/{Uri.EscapeDataString(_opt.AppId)}/authorize?state={Uri.EscapeDataString(state)}";
    }

    public async Task<NuvemshopTokenResponse?> ExchangeCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_opt.AppId) || string.IsNullOrWhiteSpace(_opt.ClientSecret))
        {
            _logger.LogError("Nuvemshop AppId/ClientSecret não configurados");
            return null;
        }

        var url = $"{_opt.AuthBaseUrl.TrimEnd('/')}/apps/authorize/token";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("User-Agent", _opt.UserAgent);
        request.Content = JsonContent.Create(new
        {
            client_id = _opt.AppId,
            client_secret = _opt.ClientSecret,
            grant_type = "authorization_code",
            code,
        });

        using var response = await _http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Falha ao trocar code Nuvemshop: {Status} {Body}", (int)response.StatusCode, body);
            return null;
        }

        return JsonSerializer.Deserialize<NuvemshopTokenResponse>(body, JsonOpts);
    }

    public static long ResolveStoreId(NuvemshopTokenResponse token)
    {
        var fromUser = NuvemshopJsonHelpers.ParseLong(token.UserId);
        if (fromUser > 0) return fromUser;
        return NuvemshopJsonHelpers.ParseLong(token.StoreId);
    }
}
