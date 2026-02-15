using Transponder.Transports.SignalR.Abstractions;

namespace Transponder.Transports.SignalR;

/// <summary>
/// Default SignalR topology conventions.
/// </summary>
public sealed class SignalRTopology : ISignalRTopology
{
    public string HubPath => "/transponder/transport";

    public string SendMethodName => "Send";

    public string PublishMethodName => "Publish";
}
