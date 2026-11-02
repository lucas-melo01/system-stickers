using System.Text.Json;
using System.Text.Json.Serialization;

namespace SistemaEtiquetas.API.DTO;

public sealed class NuvemshopWebhookPayload
{
    [JsonPropertyName("store_id")]
    public long StoreId { get; set; }

    [JsonPropertyName("event")]
    public string? Event { get; set; }

    [JsonPropertyName("id")]
    public JsonElement Id { get; set; }
}

public sealed class NuvemshopTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("user_id")]
    public JsonElement UserId { get; set; }

    [JsonPropertyName("store_id")]
    public JsonElement StoreId { get; set; }
}

public sealed class NuvemshopOrderDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("number")]
    public long Number { get; set; }

    [JsonPropertyName("contact_name")]
    public string? ContactName { get; set; }

    [JsonPropertyName("contact_identification")]
    public string? ContactIdentification { get; set; }

    [JsonPropertyName("payment_status")]
    public string? PaymentStatus { get; set; }

    [JsonPropertyName("gateway_name")]
    public string? GatewayName { get; set; }

    [JsonPropertyName("created_at")]
    [JsonConverter(typeof(NuvemshopNullableDateTimeConverter))]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("paid_at")]
    [JsonConverter(typeof(NuvemshopNullableDateTimeConverter))]
    public DateTime? PaidAt { get; set; }

    [JsonPropertyName("payment_details")]
    public NuvemshopPaymentDetailsDto? PaymentDetails { get; set; }

    [JsonPropertyName("products")]
    public List<NuvemshopOrderProductDto>? Products { get; set; }

    [JsonPropertyName("fulfillments")]
    public List<NuvemshopFulfillmentDto>? Fulfillments { get; set; }

    [JsonPropertyName("shipping_address")]
    public NuvemshopShippingAddressDto? ShippingAddress { get; set; }
}

public sealed class NuvemshopPaymentDetailsDto
{
    [JsonPropertyName("method")]
    public string? Method { get; set; }
}

public sealed class NuvemshopOrderProductDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("price")]
    public JsonElement Price { get; set; }

    [JsonPropertyName("quantity")]
    public JsonElement Quantity { get; set; }

    [JsonPropertyName("variant_values")]
    public List<JsonElement>? VariantValues { get; set; }
}

public sealed class NuvemshopFulfillmentDto
{
    [JsonPropertyName("shipping")]
    public NuvemshopFulfillmentShippingDto? Shipping { get; set; }

    [JsonPropertyName("shipping_cost")]
    public JsonElement ShippingCost { get; set; }

    [JsonPropertyName("shipping_option")]
    public JsonElement ShippingOption { get; set; }

    [JsonPropertyName("shipping_method")]
    public NuvemshopShippingMethodDto? ShippingMethod { get; set; }
}

public sealed class NuvemshopFulfillmentShippingDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("carrier")]
    public NuvemshopCarrierDto? Carrier { get; set; }

    [JsonPropertyName("option")]
    public JsonElement Option { get; set; }

    [JsonPropertyName("cost")]
    public JsonElement Cost { get; set; }
}

public sealed class NuvemshopCarrierDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class NuvemshopShippingMethodDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

public sealed class NuvemshopShippingAddressDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class NuvemshopWebhookRegistration
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("event")]
    public string? Event { get; set; }
}

public static class NuvemshopJsonHelpers
{
    public static int ParseInt(JsonElement el, int fallback = 1)
    {
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n))
            return n;
        if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var s))
            return s;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var d))
            return (int)Math.Round(d);
        return fallback;
    }

    public static decimal ParseDecimal(JsonElement el, decimal fallback = 0m)
    {
        if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var n))
            return n;
        if (el.ValueKind == JsonValueKind.String
            && decimal.TryParse(el.GetString(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var s))
            return s;
        return fallback;
    }

    public static long ParseLong(JsonElement el, long fallback = 0)
    {
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt64(out var n))
            return n;
        if (el.ValueKind == JsonValueKind.String && long.TryParse(el.GetString(), out var s))
            return s;
        return fallback;
    }

    public static string? VariantValueAsString(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.ToString(),
            JsonValueKind.Object when el.TryGetProperty("pt", out var pt) => pt.GetString(),
            JsonValueKind.Object when el.TryGetProperty("en", out var en) => en.GetString(),
            JsonValueKind.Object when el.TryGetProperty("es", out var es) => es.GetString(),
            _ => el.ToString(),
        };
    }

    /// <summary>
    /// Extrai texto de campos que a Nuvemshop manda ora como string, ora como objeto
    /// (ex.: <c>shipping.option</c>).
    /// </summary>
    public static string? AsDisplayString(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.Undefined or JsonValueKind.Null => null,
            JsonValueKind.String => NullIfWhiteSpace(el.GetString()),
            JsonValueKind.Number => el.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Object => ReadObjectDisplayName(el),
            _ => null,
        };
    }

    private static string? ReadObjectDisplayName(JsonElement obj)
    {
        foreach (var key in new[] { "name", "code", "option", "label", "title" })
        {
            if (!obj.TryGetProperty(key, out var prop))
                continue;

            if (prop.ValueKind == JsonValueKind.String)
            {
                var s = NullIfWhiteSpace(prop.GetString());
                if (s != null) return s;
            }
            else if (prop.ValueKind == JsonValueKind.Object)
            {
                // name: { "pt": "...", "en": "..." }
                foreach (var lang in new[] { "pt", "en", "es" })
                {
                    if (prop.TryGetProperty(lang, out var localized))
                    {
                        var s = NullIfWhiteSpace(localized.GetString());
                        if (s != null) return s;
                    }
                }
            }
            else if (prop.ValueKind == JsonValueKind.Number)
            {
                return prop.ToString();
            }
        }

        foreach (var lang in new[] { "pt", "en", "es" })
        {
            if (obj.TryGetProperty(lang, out var localized))
            {
                var s = NullIfWhiteSpace(localized.GetString());
                if (s != null) return s;
            }
        }

        return null;
    }

    private static string? NullIfWhiteSpace(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
