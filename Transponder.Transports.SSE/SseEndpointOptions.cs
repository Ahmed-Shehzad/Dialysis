namespace Transponder.Transports.SSE;

/// <summary>
/// Options for the SSE endpoint.
/// </summary>
public sealed class SseEndpointOptions
{
    public string? Path { get; set; }

    public string StreamQueryKey { get; set; } = "stream";

    public string GroupQueryKey { get; set; } = "group";

    public string UserQueryKey { get; set; } = "user";

    public bool RequireEventStreamAcceptHeader { get; set; }

    public bool SendConnectionEventOnConnect { get; set; } = true;

    public string ConnectionEventName { get; set; } = "connection";
}
