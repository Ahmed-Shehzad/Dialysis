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

        byte[] key = Encoding.UTF8.GetBytes(secret);
        byte[] timestampBytes = Encoding.UTF8.GetBytes(timestamp);

        byte[] signedPayload = new byte[timestampBytes.Length + 1 + payload.Length];
        Buffer.BlockCopy(timestampBytes, 0, signedPayload, 0, timestampBytes.Length);
        signedPayload[timestampBytes.Length] = (byte)'.';
        Buffer.BlockCopy(payload, 0, signedPayload, timestampBytes.Length + 1, payload.Length);

        using var hmac = new HMACSHA256(key);
        byte[] hash = hmac.ComputeHash(signedPayload);
        string hex = Convert.ToHexString(hash).ToLowerInvariant();

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

        string expected = Compute(secret, timestamp, payload, options);
        return FixedTimeEquals(providedSignature, expected);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        byte[] leftBytes = Encoding.UTF8.GetBytes(left);
        byte[] rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
