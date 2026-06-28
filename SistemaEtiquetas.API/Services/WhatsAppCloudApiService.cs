using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SistemaEtiquetas.API.Services;

public sealed class WhatsAppCloudApiService
{
    private readonly HttpClient _http;
    private readonly WhatsAppApiOptions _opt;
    private readonly ILogger<WhatsAppCloudApiService> _logger;

    public WhatsAppCloudApiService(
        HttpClient http,
        IOptions<WhatsAppApiOptions> opt,
        ILogger<WhatsAppCloudApiService> logger)
    {
        _http = http;
        _opt = opt.Value;
        _logger = logger;
    }

    public async Task<(bool ok, string? messageId, string? error)> EnviarTemplateEncomendaAsync(
        string whatsAppDestino,
        string? imagemUrl,
        string nomeProduto,
        string cor,
        string tamanho,
        string codigoFornecedor,
        string nomeCliente,
        string pedidoExternoId,
        CancellationToken cancellationToken = default)
    {
        var phoneId = (_opt.PhoneNumberId ?? "").Trim();
        var token = (_opt.AccessToken ?? "").Trim();
        var template = (_opt.TemplateName ?? "").Trim();
        if (phoneId.Length == 0 || token.Length == 0 || template.Length == 0)
            return (false, null, "WhatsApp não configurado (PhoneNumberId, AccessToken ou TemplateName ausente).");

        var to = new string(whatsAppDestino.Where(char.IsDigit).ToArray());
        if (to.Length < 10)
            return (false, null, "WhatsApp do fornecedor inválido.");

        var components = new List<object>();

        if (!string.IsNullOrWhiteSpace(imagemUrl))
        {
            components.Add(new
            {
                type = "header",
                parameters = new[]
                {
                    new { type = "image", image = new { link = imagemUrl.Trim() } },
                },
            });
        }

        components.Add(new
        {
            type = "body",
            parameters = new object[]
            {
                new { type = "text", text = Truncar(nomeProduto, 200) },
                new { type = "text", text = Truncar(cor, 100) },
                new { type = "text", text = Truncar(tamanho, 100) },
                new { type = "text", text = Truncar(codigoFornecedor, 100) },
                new { type = "text", text = Truncar(nomeCliente, 100) },
                new { type = "text", text = Truncar(pedidoExternoId, 50) },
            },
        });

        var payload = new
        {
            messaging_product = "whatsapp",
            to,
            type = "template",
            template = new
            {
                name = template,
                language = new { code = _opt.TemplateLanguage ?? "pt_BR" },
                components,
            },
        };

        var version = string.IsNullOrWhiteSpace(_opt.GraphApiVersion) ? "v21.0" : _opt.GraphApiVersion.Trim();
        var url = $"https://graph.facebook.com/{version}/{phoneId}/messages";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            var response = await _http.SendAsync(req, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("WhatsApp API HTTP {Status}: {Body}", (int)response.StatusCode, body);
                return (false, null, $"Meta API {(int)response.StatusCode}: {body}");
            }

            using var doc = JsonDocument.Parse(body);
            var messageId = doc.RootElement.TryGetProperty("messages", out var msgs)
                && msgs.GetArrayLength() > 0
                && msgs[0].TryGetProperty("id", out var idEl)
                ? idEl.GetString()
                : null;
            return (true, messageId, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar WhatsApp");
            return (false, null, ex.Message);
        }
    }

    private static string Truncar(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "-" : (s.Length <= max ? s : s[..max]);
}
