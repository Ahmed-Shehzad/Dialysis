namespace Transponder.Transports.Abstractions;

/// <summary>
/// Defines retry, circuit breaker, and dead-letter settings for a receive endpoint.
/// </summary>
public sealed class ReceiveEndpointFaultSettings
{
    public ReceiveEndpointFaultSettings(
        TransportResilienceOptions? resilienceOptions = null,
        Uri? deadLetterAddress = null,
        bool useTransportDeadLetter = false,
        string? deadLetterReason = null,
        string? deadLetterDescription = null)
    {
        ResilienceOptions = resilienceOptions;
        DeadLetterAddress = deadLetterAddress;
        UseTransportDeadLetter = useTransportDeadLetter;
        DeadLetterReason = deadLetterReason;
        DeadLetterDescription = deadLetterDescription;
    }

    /// <summary>
    /// Gets the resilience settings to apply to message handling.
    /// </summary>
    public TransportResilienceOptions? ResilienceOptions { get; }

    /// <summary>
    /// Gets the dead-letter address for failed messages, if configured.
    /// </summary>
    public Uri? DeadLetterAddress { get; }

    /// <summary>
    /// Gets whether to use the transport's native dead-letter capability.
    /// </summary>
    public bool UseTransportDeadLetter { get; }

    /// <summary>
    /// Gets the dead-letter reason, if supported by the transport.
    /// </summary>
    public string? DeadLetterReason { get; }

    /// <summary>
    /// Gets the dead-letter description, if supported by the transport.
    /// </summary>
    public string? DeadLetterDescription { get; }
}
