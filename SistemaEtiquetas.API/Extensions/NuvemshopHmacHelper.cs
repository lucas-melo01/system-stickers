using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SistemaEtiquetas.API.Services;

namespace SistemaEtiquetas.API.Extensions;

public static class NuvemshopHmacHelper
{
    public const string HmacHeaderName = "x-linkedstore-hmac-sha256";

    public static bool Validate(string? clientSecret, string rawBody, string? hmacHeader)
    {
        if (string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(hmacHeader))
            return false;

        var expected = ComputeHmacHex(clientSecret, rawBody);
        var provided = hmacHeader.Trim().ToLowerInvariant();
        if (expected.Length != provided.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(provided));
    }

    public static bool Validate(IOptions<NuvemshopOptions> options, string rawBody, IHeaderDictionary headers)
    {
        var secret = options.Value.ClientSecret;
        var hmac = headers[HmacHeaderName].FirstOrDefault()
            ?? headers["X-Linkedstore-Hmac-Sha256"].FirstOrDefault();
        return Validate(secret, rawBody, hmac);
    }

    public static string ComputeHmacHex(string clientSecret, string rawBody)
    {
        var key = Encoding.UTF8.GetBytes(clientSecret);
        var data = Encoding.UTF8.GetBytes(rawBody);
        var hash = HMACSHA256.HashData(key, data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
