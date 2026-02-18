namespace Transponder.Transports.Webhooks;

/// <summary>
/// Well-known webhook header keys used by Transponder.
/// </summary>
public static class TransponderWebhookHeaders
{
    public const string EventId = "X-Transponder-Id";
    public const string EventType = "X-Transponder-Type";
    public const string SentTime = "X-Transponder-SentAt";
    public const string ContentType = "X-Transponder-Content-Type";
    public const string CorrelationId = "X-Transponder-Correlation";
    public const string ConversationId = "X-Transponder-Conversation";
    public const string Subscription = "X-Transponder-Subscription";
}
