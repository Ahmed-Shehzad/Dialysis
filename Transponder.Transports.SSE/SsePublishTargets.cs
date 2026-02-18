namespace Transponder.Transports.SSE;

/// <summary>
/// Represents the set of SSE publish targets for a message.
/// </summary>
public sealed class SsePublishTargets
{
    public SsePublishTargets(
        bool broadcast,
        IReadOnlyList<string> connectionIds,
        IReadOnlyList<string> excludedConnectionIds,
        IReadOnlyList<string> streams,
        IReadOnlyList<string> groups,
        IReadOnlyList<string> users)
    {
        Broadcast = broadcast;
        ConnectionIds = connectionIds ?? Array.Empty<string>();
        ExcludedConnectionIds = excludedConnectionIds ?? Array.Empty<string>();
        Streams = streams ?? Array.Empty<string>();
        Groups = groups ?? Array.Empty<string>();
        Users = users ?? Array.Empty<string>();
    }

    public bool Broadcast { get; }

    public IReadOnlyList<string> ConnectionIds { get; }

    public IReadOnlyList<string> ExcludedConnectionIds { get; }

    public IReadOnlyList<string> Streams { get; }

    public IReadOnlyList<string> Groups { get; }

    public IReadOnlyList<string> Users { get; }
}
