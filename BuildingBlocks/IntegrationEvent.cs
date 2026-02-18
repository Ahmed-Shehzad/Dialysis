using BuildingBlocks.Abstractions;

namespace BuildingBlocks;

/// <summary>
/// Base type for integration events with standard metadata.
/// </summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    protected IntegrationEvent(Ulid correlationId)
    {
        CorrelationId = correlationId;
        EventId = Ulid.NewUlid();
        OccurredOn = DateTime.UtcNow;
    }

    public Ulid EventId { get; init; }
    public DateTimeOffset OccurredOn { get; init; }
    public Ulid CorrelationId { get; }
}
