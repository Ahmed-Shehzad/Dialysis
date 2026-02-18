namespace Transponder.Transports.SignalR;

/// <summary>
/// Represents the set of SignalR publish targets for a message.
/// </summary>
public sealed class SignalRPublishTargets
{
    public SignalRPublishTargets(
        bool broadcast,
        IReadOnlyList<string> connectionIds,
        IReadOnlyList<string> excludedConnectionIds,
        IReadOnlyList<string> groups,
        IReadOnlyList<string> users)
    {
        Broadcast = broadcast;
        ConnectionIds = connectionIds ?? Array.Empty<string>();
        ExcludedConnectionIds = excludedConnectionIds ?? Array.Empty<string>();
        Groups = groups ?? Array.Empty<string>();
        Users = users ?? Array.Empty<string>();
    }

    public bool Broadcast { get; }

    public IReadOnlyList<string> ConnectionIds { get; }

    public IReadOnlyList<string> ExcludedConnectionIds { get; }

    public IReadOnlyList<string> Groups { get; }

    public IReadOnlyList<string> Users { get; }
}
