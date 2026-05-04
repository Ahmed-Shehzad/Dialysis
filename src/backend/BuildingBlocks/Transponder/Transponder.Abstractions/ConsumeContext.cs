namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Per-delivery context for a consumer. Carries the message, cancellation, and the bus for follow-up publishes.
/// </summary>
/// <typeparam name="TMessage">The message contract type.</typeparam>
public sealed class ConsumeContext<TMessage>
    where TMessage : class
{
    public ConsumeContext(
        TMessage message,
        CancellationToken cancellationToken,
        ITransponderBus bus,
        string? correlationId = null,
        string? deduplicationId = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        CancellationToken = cancellationToken;
        Bus = bus ?? throw new ArgumentNullException(nameof(bus));
        CorrelationId = correlationId;
        DeduplicationId = deduplicationId;
    }

    public TMessage Message { get; }

    public CancellationToken CancellationToken { get; }

    public ITransponderBus Bus { get; }

    /// <summary>Correlation id from the inbound transport, when provided.</summary>
    public string? CorrelationId { get; }

    /// <summary>Stable id for inbox deduplication when the transport supplied one.</summary>
    public string? DeduplicationId { get; }
}
