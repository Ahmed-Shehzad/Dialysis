namespace Transponder.Transports.SignalR;

/// <summary>
/// Transport envelope sent over SignalR.
/// </summary>
public sealed class SignalRTransportEnvelope
{
    private SignalRTransportEnvelope(
        Transponder.Transports.MessageIdentity identity,
        string? messageType,
        string? contentType,
        DateTimeOffset? sentTime,
        IReadOnlyDictionary<string, string?> headers,
        byte[] body)
    {
        ArgumentNullException.ThrowIfNull(identity);

        MessageId = identity.MessageId;
        CorrelationId = identity.CorrelationId;
        ConversationId = identity.ConversationId;
        MessageType = messageType;
        ContentType = contentType;
        SentTime = sentTime;
        Headers = headers;
        Body = body;
    }

    public Ulid? MessageId { get; }

    public Ulid? CorrelationId { get; }

    public Ulid? ConversationId { get; }

    public string? MessageType { get; }

    public string? ContentType { get; }

    public DateTimeOffset? SentTime { get; }

    public IReadOnlyDictionary<string, string?> Headers { get; }

    public byte[] Body { get; }

    public static SignalRTransportEnvelope From(Transponder.Transports.Abstractions.ITransportMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var headers = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, object?> entry in message.Headers)
            headers[entry.Key] = entry.Value?.ToString();

        return new SignalRTransportEnvelope(
            new Transponder.Transports.MessageIdentity(message.MessageId, message.CorrelationId, message.ConversationId),
            message.MessageType,
            message.ContentType,
            message.SentTime,
            headers,
            message.Body.Span.ToArray());
    }
}
