namespace Transponder.Transports.SSE;

/// <summary>
/// Well-known header keys for SSE publish targets.
/// </summary>
public static class TransponderSseHeaders
{
    public const string Broadcast = "Sse.Broadcast";
    public const string ConnectionId = "Sse.ConnectionId";
    public const string ConnectionIds = "Sse.ConnectionIds";
    public const string ExcludeConnectionId = "Sse.ExcludeConnectionId";
    public const string ExcludeConnectionIds = "Sse.ExcludeConnectionIds";
    public const string Stream = "Sse.Stream";
    public const string Streams = "Sse.Streams";
    public const string Group = "Sse.Group";
    public const string Groups = "Sse.Groups";
    public const string User = "Sse.User";
    public const string Users = "Sse.Users";
    public const string EventName = "Sse.EventName";
    public const string EventId = "Sse.EventId";
}
