namespace Dialysis.DomainDrivenDesign.IntegrationEvents;

/// <summary>
/// Base for immutable integration events (cross-context, infrastructure-facing payloads).
/// Concrete events declare their <see cref="IIntegrationEvent.SchemaVersion"/> via the init-only
/// <see cref="SchemaVersion"/> property; defaults to 1 for first-version events.
/// </summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    protected IntegrationEvent()
    {
        EventId = Guid.CreateVersion7();
        OccurredOn = DateTime.UtcNow;
    }

    public Guid EventId { get; init; }

    public DateTime OccurredOn { get; init; }

    public int SchemaVersion { get; init; } = 1;
}
