namespace Transponder.Transports.SSE.Abstractions;

/// <summary>
/// Provides catch-up events for reconnecting SSE clients.
/// </summary>
public interface ISseCatchUpProvider
{
    Task<IReadOnlyList<SseCatchUpEvent>> GetEventsAsync(
        SseCatchUpRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a catch-up request for SSE clients.
/// </summary>
public sealed class SseCatchUpRequest
{
    public SseCatchUpRequest(
        string? lastEventId,
        string? userId,
        IReadOnlyList<string> streams,
        IReadOnlyList<string> groups)
    {
        LastEventId = lastEventId;
        UserId = userId;
        Streams = streams ?? Array.Empty<string>();
        Groups = groups ?? Array.Empty<string>();
    }

    public string? LastEventId { get; }

    public string? UserId { get; }

    public IReadOnlyList<string> Streams { get; }

    public IReadOnlyList<string> Groups { get; }
}

/// <summary>
/// Represents a catch-up event to be replayed over SSE.
/// </summary>
public sealed class SseCatchUpEvent
{
    public SseCatchUpEvent(string data, string? id = null, string? eventName = null)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Id = id;
        EventName = eventName;
    }

    public string? Id { get; }

    public string? EventName { get; }

    public string Data { get; }
}
