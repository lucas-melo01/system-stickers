namespace SistemaEtiquetas.API.Extensions;

public static class WebhookSecretHelper
{
    public static bool Validate(IConfiguration config, IHeaderDictionary headers)
    {
        var secret = config["Webhooks:Secret"] ?? Environment.GetEnvironmentVariable("WEBHOOK_SECRET");
        if (string.IsNullOrEmpty(secret))
            return true;

        if (!headers.TryGetValue("X-Webhook-Secret", out var sent))
            return false;

        return string.Equals(sent.ToString(), secret, StringComparison.Ordinal);
    }
}
