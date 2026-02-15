using System.Security.Cryptography;
using System.Text;

namespace Transponder.Transports.Webhooks;

internal static class WebhookSignature
{
    public static string Compute(
        string secret,
        string timestamp,
        byte[] payload,
        WebhookSignatureOptions options)
    {
        ArgumentNullException.ThrowIfNull(secret);
        ArgumentNullException.ThrowIfNull(timestamp);
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(options);

        var key = Encoding.UTF8.GetBytes(secret);
        var timestampBytes = Encoding.UTF8.GetBytes(timestamp);

        var signedPayload = new byte[timestampBytes.Length + 1 + payload.Length];
        Buffer.BlockCopy(timestampBytes, 0, signedPayload, 0, timestampBytes.Length);
        signedPayload[timestampBytes.Length] = (byte)'.';
        Buffer.BlockCopy(payload, 0, signedPayload, timestampBytes.Length + 1, payload.Length);

        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(signedPayload);
        var hex = Convert.ToHexString(hash).ToLowerInvariant();

        return string.Concat(options.SignaturePrefix, hex);
    }

    public static bool Verify(
        string providedSignature,
        string secret,
        string timestamp,
        byte[] payload,
        WebhookSignatureOptions options)
    {
        if (string.IsNullOrWhiteSpace(providedSignature)) return false;

        var expected = Compute(secret, timestamp, payload, options);
        return FixedTimeEquals(providedSignature, expected);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
