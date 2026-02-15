namespace Transponder.Transports.Abstractions;

/// <summary>
/// Extends receive endpoint configuration with fault handling settings.
/// </summary>
public interface IReceiveEndpointConfigurationWithFaults : IReceiveEndpointConfiguration
{
    /// <summary>
    /// Gets the fault settings for the receive endpoint.
    /// </summary>
    ReceiveEndpointFaultSettings? FaultSettings { get; }
}
