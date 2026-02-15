namespace Transponder.Transports.SignalR.Abstractions;

/// <summary>
/// Provides SignalR topology conventions.
/// </summary>
public interface ISignalRTopology
{
    /// <summary>
    /// Gets the SignalR hub path.
    /// </summary>
    string HubPath { get; }

    /// <summary>
    /// Gets the hub method name for send operations.
    /// </summary>
    string SendMethodName { get; }

    /// <summary>
    /// Gets the hub method name for publish operations.
    /// </summary>
    string PublishMethodName { get; }
}
