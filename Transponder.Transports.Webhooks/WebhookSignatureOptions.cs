namespace Transponder.Transports.Webhooks;

/// <summary>
/// Options for webhook signature headers.
/// </summary>
public sealed class WebhookSignatureOptions
{
    public string SignatureHeaderName { get; init; } = "X-Transponder-Signature";

    public string TimestampHeaderName { get; init; } = "X-Transponder-Timestamp";

    public string SignaturePrefix { get; init; } = "sha256=";
}
