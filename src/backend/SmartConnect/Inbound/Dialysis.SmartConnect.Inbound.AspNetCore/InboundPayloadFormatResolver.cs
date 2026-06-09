namespace Dialysis.SmartConnect.Inbound.AspNetCore;

internal static class InboundPayloadFormatResolver
{
    internal const string PayloadFormatHeaderName = "X-SmartConnect-Payload-Format";

    /// <summary>
    /// Resolves format from <paramref name="explicitHeader"/> first, then <paramref name="contentType"/>.
    /// </summary>
    public static PayloadFormat Resolve(string? explicitHeader, string? contentType)
    {
        if (!string.IsNullOrWhiteSpace(explicitHeader)
            && Enum.TryParse<PayloadFormat>(explicitHeader.Trim(), ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        if (string.IsNullOrWhiteSpace(contentType))
            return PayloadFormat.Binary;

        var ct = contentType.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0];
        return ct.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            ? PayloadFormat.Json
            : ct.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
                ? PayloadFormat.Utf8Text
                : PayloadFormat.Binary;
    }
}
