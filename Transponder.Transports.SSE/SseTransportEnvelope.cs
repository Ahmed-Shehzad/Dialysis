using System.Text.Json;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports.SSE;

/// <summary>
/// Transport envelope sent over SSE.
/// </summary>
public sealed class SseTransportEnvelope
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal SseTransportEnvelope(
        string? id,
        string? messageType,
        string? contentType,
        DateTimeOffset? sentTime,
        IReadOnlyDictionary<string, string?> headers,
        string body)
    {
        Id = id;
        MessageType = messageType;
        ContentType = contentType;
        SentTime = sentTime;
        Headers = headers;
        Body = body;
    }

    public string? Id { get; }

    public string? MessageType { get; }

    public string? ContentType { get; }

    public DateTimeOffset? SentTime { get; }

    public IReadOnlyDictionary<string, string?> Headers { get; }

    public string Body { get; }

    public static SseTransportEnvelope From(ITransportMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var headers = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, object?> entry in message.Headers)
            headers[entry.Key] = entry.Value?.ToString();

        string body = Convert.ToBase64String(message.Body.Span);

        return new SseTransportEnvelope(
            message.MessageId?.ToString(),
            message.MessageType,
            message.ContentType,
            message.SentTime,
            headers,
            body);
    }

    public SseTransportEnvelope WithId(string? id)
        => new(
            string.IsNullOrWhiteSpace(id) ? Id : id,
            MessageType,
            ContentType,
            SentTime,
            Headers,
            Body);

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);
}
