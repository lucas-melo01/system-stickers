using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SistemaEtiquetas.API.DTO;

namespace SistemaEtiquetas.API.Services;

public sealed class NuvemshopApiClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly NuvemshopOptions _opt;
    private readonly ILogger<NuvemshopApiClient> _logger;

    public NuvemshopApiClient(
        HttpClient http,
        IOptions<NuvemshopOptions> opt,
        ILogger<NuvemshopApiClient> logger)
    {
        _http = http;
        _opt = opt.Value;
        _logger = logger;
        _http.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<(NuvemshopOrderDto? Order, string? RawJson)> GetOrderAsync(
        long storeId,
        long orderId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var url = $"{ApiRoot(storeId)}/orders/{orderId}?aggregates=fulfillment_orders";
        using var response = await SendWithRetryAsync(HttpMethod.Get, url, accessToken, null, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "GET order Nuvemshop falhou store={StoreId} order={OrderId} status={Status} body={Body}",
                storeId, orderId, (int)response.StatusCode, Truncate(body));
            return (null, null);
        }

        var order = JsonSerializer.Deserialize<NuvemshopOrderDto>(body, JsonOpts);
        return (order, body);
    }

    public async Task EnsureWebhooksAsync(
        long storeId,
        string accessToken,
        string webhookBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = webhookBaseUrl.TrimEnd('/');
        var desired = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["order/paid"] = $"{baseUrl}/webhook/nuvemshop",
            ["app/uninstalled"] = $"{baseUrl}/webhook/nuvemshop",
            ["app/suspended"] = $"{baseUrl}/webhook/nuvemshop",
            ["app/resumed"] = $"{baseUrl}/webhook/nuvemshop",
        };

        var existing = await ListWebhooksAsync(storeId, accessToken, cancellationToken);
        foreach (var (eventName, url) in desired)
        {
            var match = existing.FirstOrDefault(w =>
                string.Equals(w.Event, eventName, StringComparison.OrdinalIgnoreCase));
            if (match != null
                && string.Equals(match.Url?.TrimEnd('/'), url.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                continue;

            if (match != null)
                await DeleteWebhookAsync(storeId, accessToken, match.Id, cancellationToken);

            await CreateWebhookAsync(storeId, accessToken, eventName, url, cancellationToken);
        }
    }

    public async Task<List<NuvemshopWebhookRegistration>> ListWebhooksAsync(
        long storeId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var url = $"{ApiRoot(storeId)}/webhooks";
        using var response = await SendWithRetryAsync(HttpMethod.Get, url, accessToken, null, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "List webhooks Nuvemshop falhou store={StoreId} status={Status}",
                storeId, (int)response.StatusCode);
            return [];
        }

        return JsonSerializer.Deserialize<List<NuvemshopWebhookRegistration>>(body, JsonOpts) ?? [];
    }

    private async Task CreateWebhookAsync(
        long storeId,
        string accessToken,
        string eventName,
        string webhookUrl,
        CancellationToken cancellationToken)
    {
        var endpoint = $"{ApiRoot(storeId)}/webhooks";
        var json = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["event"] = eventName,
            ["url"] = webhookUrl,
        });

        using var response = await SendWithRetryAsync(
            HttpMethod.Post, endpoint, accessToken, json, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Create webhook Nuvemshop falhou event={Event} status={Status} body={Body}",
                eventName, (int)response.StatusCode, Truncate(body));
            return;
        }

        _logger.LogInformation("Webhook Nuvemshop registrado: {Event} → {Url}", eventName, webhookUrl);
    }

    private async Task DeleteWebhookAsync(
        long storeId,
        string accessToken,
        long webhookId,
        CancellationToken cancellationToken)
    {
        var url = $"{ApiRoot(storeId)}/webhooks/{webhookId}";
        using var response = await SendWithRetryAsync(HttpMethod.Delete, url, accessToken, null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Delete webhook Nuvemshop falhou id={Id} status={Status}",
                webhookId, (int)response.StatusCode);
        }
    }

    private string ApiRoot(long storeId) =>
        $"{_opt.ApiBaseUrl.TrimEnd('/')}/{storeId}";

    private HttpRequestMessage BuildRequest(HttpMethod method, string url, string accessToken, string? jsonBody)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.TryAddWithoutValidation("User-Agent", _opt.UserAgent);
        if (jsonBody != null)
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        return request;
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        HttpMethod method,
        string url,
        string accessToken,
        string? jsonBody,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            using var request = BuildRequest(method, url, accessToken, jsonBody);
            var response = await _http.SendAsync(request, cancellationToken);
            if ((int)response.StatusCode != 429)
                return response;

            response.Dispose();
            var delayMs = 500 * (attempt + 1);
            _logger.LogWarning("Nuvemshop 429; retry em {Delay}ms", delayMs);
            await Task.Delay(delayMs, cancellationToken);
        }

        using var last = BuildRequest(method, url, accessToken, jsonBody);
        return await _http.SendAsync(last, cancellationToken);
    }

    private static string Truncate(string? s, int max = 400) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..max];
}
