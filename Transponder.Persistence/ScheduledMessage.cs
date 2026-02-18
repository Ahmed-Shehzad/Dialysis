using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence;

/// <summary>
/// Default scheduled message implementation.
/// </summary>
public sealed class ScheduledMessage : IScheduledMessage
{
    private readonly byte[] _body;
    private readonly IReadOnlyDictionary<string, object?> _headers;

    public ScheduledMessage(
        Ulid tokenId,
        string messageType,
        ReadOnlyMemory<byte> body,
        ScheduledMessageTimestamps timestamps,
        IReadOnlyDictionary<string, object?>? headers = null,
        string? contentType = null)
    {
        if (tokenId == Ulid.Empty) throw new ArgumentException("TokenId must be provided.", nameof(tokenId));

        if (string.IsNullOrWhiteSpace(messageType)) throw new ArgumentException("MessageType must be provided.", nameof(messageType));

        ArgumentNullException.ThrowIfNull(timestamps);

        TokenId = tokenId;
        MessageType = messageType;
        ContentType = contentType;
        _body = body.ToArray();
        _headers = headers != null
            ? new Dictionary<string, object?>(headers, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        ScheduledTime = timestamps.ScheduledTime;
        CreatedTime = timestamps.CreatedTime ?? DateTimeOffset.UtcNow;
        DispatchedTime = timestamps.DispatchedTime;
    }

    /// <inheritdoc />
    public Ulid TokenId { get; }

    /// <inheritdoc />
    public string MessageType { get; }

    /// <inheritdoc />
    public string? ContentType { get; }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> Body => _body;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Headers => _headers;

    /// <inheritdoc />
    public DateTimeOffset ScheduledTime { get; }

    /// <inheritdoc />
    public DateTimeOffset CreatedTime { get; }

    /// <inheritdoc />
    public DateTimeOffset? DispatchedTime { get; private set; }

    internal void MarkDispatched(DateTimeOffset dispatchedTime) => DispatchedTime = dispatchedTime;

    public static ScheduledMessage FromMessage(IScheduledMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new ScheduledMessage(
            message.TokenId,
            message.MessageType,
            message.Body,
            new ScheduledMessageTimestamps(message.ScheduledTime, message.CreatedTime, message.DispatchedTime),
            message.Headers,
            message.ContentType);
    }
}
