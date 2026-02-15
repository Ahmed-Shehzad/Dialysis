namespace Transponder.Transports.Abstractions;

/// <summary>
/// Provides resilience settings for transport hosts.
/// </summary>
public interface ITransportHostResilienceSettings : ITransportHostSettings
{
    /// <summary>
    /// Gets the resilience settings for transport operations.
    /// </summary>
    TransportResilienceOptions? ResilienceOptions { get; }
}
