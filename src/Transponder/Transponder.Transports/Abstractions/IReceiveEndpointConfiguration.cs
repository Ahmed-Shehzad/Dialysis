namespace Transponder.Transports.Abstractions;

/// <summary>
/// Defines configuration required to connect a receive endpoint.
/// </summary>
public interface IReceiveEndpointConfiguration
{
    /// <summary>
    /// Gets the input address for the endpoint.
    /// </summary>
    Uri InputAddress { get; }

    /// <summary>
    /// Gets the handler that processes received transport messages.
    /// </summary>
    Func<IReceiveContext, Task> Handler { get; }

    /// <summary>
    /// Gets transport-specific endpoint settings.
    /// </summary>
    IReadOnlyDictionary<string, object?> Settings { get; }
}
