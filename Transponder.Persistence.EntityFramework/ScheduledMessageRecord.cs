using System.Text.Json;

using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence.EntityFramework;

/// <summary>
/// EF Core persistence model for scheduled messages.
/// </summary>
public sealed class ScheduledMessageRecord : IScheduledMessage
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private IReadOnlyDictionary<string, object?>? _headersCache;
    private string? _headers;

    public Ulid TokenId { get; set; }

    public string MessageType { get; set; } = string.Empty;

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

    public DateTimeOffset ScheduledTime { get; set; }

    public DateTimeOffset CreatedTime { get; set; }

    public DateTimeOffset? DispatchedTime { get; set; }

    ReadOnlyMemory<byte> IScheduledMessage.Body => Body;

    IReadOnlyDictionary<string, object?> IScheduledMessage.Headers => GetHeaders();

    internal static ScheduledMessageRecord FromMessage(IScheduledMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new ScheduledMessageRecord
        {
            TokenId = message.TokenId,
            MessageType = message.MessageType,
            ContentType = message.ContentType,
            Body = message.Body.ToArray(),
            Headers = SerializeHeaders(message.Headers),
            ScheduledTime = message.ScheduledTime,
            CreatedTime = message.CreatedTime,
            DispatchedTime = message.DispatchedTime
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
}
