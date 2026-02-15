namespace Transponder.Transports.SignalR;

/// <summary>
/// Well-known header keys for SignalR publish targets.
/// </summary>
public static class TransponderSignalRHeaders
{
    public const string Broadcast = "SignalR.Broadcast";
    public const string ConnectionId = "SignalR.ConnectionId";
    public const string ConnectionIds = "SignalR.ConnectionIds";
    public const string ExcludeConnectionId = "SignalR.ExcludeConnectionId";
    public const string ExcludeConnectionIds = "SignalR.ExcludeConnectionIds";
    public const string Group = "SignalR.Group";
    public const string Groups = "SignalR.Groups";
    public const string User = "SignalR.User";
    public const string Users = "SignalR.Users";
}
