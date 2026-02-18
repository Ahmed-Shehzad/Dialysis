using System.Text.Json;

using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence.EntityFramework;

/// <summary>
/// EF Core persistence model for outbox messages.
/// </summary>
public sealed class OutboxMessageRecord : IOutboxMessage
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private IReadOnlyDictionary<string, object?>? _headersCache;
    private string? _headers;

    /// <inheritdoc />
    public Ulid MessageId { get; set; }

    /// <inheritdoc />
    public Ulid? CorrelationId { get; set; }

    /// <inheritdoc />
    public Ulid? ConversationId { get; set; }

    public string? SourceAddress { get; set; }

    public string? DestinationAddress { get; set; }

    /// <inheritdoc />
    public string? MessageType { get; set; }

    /// <inheritdoc />
    public string? ContentType { get; set; }

    public byte[] Body { get; set; } = Array.Empty<byte>();

    public string? Headers
    {
        get => _headers;
        set
        {
            _headers = value;
            _headersCache = null;
        }
    }

    /// <inheritdoc />
    public DateTimeOffset EnqueuedTime { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? SentTime { get; set; }

    ReadOnlyMemory<byte> IOutboxMessage.Body => Body;

    IReadOnlyDictionary<string, object?> IOutboxMessage.Headers => GetHeaders();

    Uri? IOutboxMessage.SourceAddress => CreateUri(SourceAddress);

    Uri? IOutboxMessage.DestinationAddress => CreateUri(DestinationAddress);

    internal static OutboxMessageRecord FromMessage(IOutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new OutboxMessageRecord
        {
            MessageId = message.MessageId,
            CorrelationId = message.CorrelationId,
            ConversationId = message.ConversationId,
            SourceAddress = message.SourceAddress?.ToString(),
            DestinationAddress = message.DestinationAddress?.ToString(),
            MessageType = message.MessageType,
            ContentType = message.ContentType,
            Body = message.Body.ToArray(),
            Headers = SerializeHeaders(message.Headers),
            EnqueuedTime = message.EnqueuedTime,
            SentTime = message.SentTime
        };
    }

    private IReadOnlyDictionary<string, object?> GetHeaders()
    {
        if (_headersCache != null) return _headersCache;

        if (string.IsNullOrWhiteSpace(_headers))
        {
            _headersCache = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            return _headersCache;
        }

        try
        {
            Dictionary<string, object?> parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(_headers, SerializerOptions)
                                                 ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            _headersCache = new Dictionary<string, object?>(parsed, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            _headersCache = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        return _headersCache;
    }

    private static string? SerializeHeaders(IReadOnlyDictionary<string, object?>? headers) => headers == null || headers.Count == 0 ? null : JsonSerializer.Serialize(headers, SerializerOptions);

    private static Uri? CreateUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out Uri? uri))
            return uri;

        return null;
    }
}
