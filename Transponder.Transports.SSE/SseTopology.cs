using Transponder.Transports.SSE.Abstractions;

namespace Transponder.Transports.SSE;

/// <summary>
/// Default SSE topology conventions.
/// </summary>
public sealed class SseTopology : ISseTopology
{
    public string StreamPath => "/transponder/stream";

    public string SendPath => "/transponder/send";

    public string PublishPath => "/transponder/publish";
}
