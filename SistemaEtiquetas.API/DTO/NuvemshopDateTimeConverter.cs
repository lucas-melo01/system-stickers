using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SistemaEtiquetas.API.DTO;

/// <summary>
/// Aceita datas da Nuvemshop no formato <c>2024-01-15T19:36:59+0000</c>
/// (offset sem dois-pontos), além de ISO 8601 padrão.
/// </summary>
public sealed partial class NuvemshopDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            throw new JsonException("Null não é válido para DateTime não-nullable.");

        if (reader.TokenType == JsonTokenType.String)
        {
            var raw = reader.GetString();
            if (TryParse(raw, out var dt))
                return dt;
            throw new JsonException($"Data Nuvemshop inválida: '{raw}'");
        }

        if (reader.TryGetDateTime(out var direct))
            return direct;

        throw new JsonException($"Token inesperado para DateTime: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture));

    internal static bool TryParse(string? raw, out DateTime value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var s = raw.Trim();
        // +0000 / -0300 → +00:00 / -03:00
        s = CompactOffsetRegex().Replace(s, "$1$2:$3");

        if (DateTime.TryParse(
                s,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed))
        {
            value = parsed.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                : parsed.ToUniversalTime();
            return true;
        }

        string[] formats =
        [
            "yyyy-MM-dd'T'HH:mm:sszzz",
            "yyyy-MM-dd'T'HH:mm:ss.fffzzz",
            "yyyy-MM-dd'T'HH:mm:ssZ",
            "yyyy-MM-dd'T'HH:mm:ss",
        ];

        if (DateTime.TryParseExact(
                s,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out parsed))
        {
            value = parsed.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                : parsed.ToUniversalTime();
            return true;
        }

        return false;
    }

    [GeneratedRegex(@"([+-])(\d{2})(\d{2})$")]
    private static partial Regex CompactOffsetRegex();
}

public sealed class NuvemshopNullableDateTimeConverter : JsonConverter<DateTime?>
{
    private static readonly NuvemshopDateTimeConverter Inner = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var raw = reader.GetString();
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            if (NuvemshopDateTimeConverter.TryParse(raw, out var dt))
                return dt;
            throw new JsonException($"Data Nuvemshop inválida: '{raw}'");
        }

        return Inner.Read(ref reader, typeof(DateTime), options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            Inner.Write(writer, value.Value, options);
    }
}
