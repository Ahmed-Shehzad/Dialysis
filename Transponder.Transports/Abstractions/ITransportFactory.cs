namespace Transponder.Transports.Abstractions;

/// <summary>
/// Creates transport hosts for a specific transport implementation.
/// </summary>
public interface ITransportFactory
{
    /// <summary>
    /// Gets the transport name (e.g. RabbitMQ, Azure Service Bus).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the URI schemes supported by this transport (e.g. "rabbitmq", "sb").
    /// </summary>
    IReadOnlyCollection<string> SupportedSchemes { get; }

    /// <summary>
    /// Creates a transport host using the provided settings.
    /// </summary>
    /// <param name="settings">The transport host settings.</param>
    ITransportHost CreateHost(ITransportHostSettings settings);
}
