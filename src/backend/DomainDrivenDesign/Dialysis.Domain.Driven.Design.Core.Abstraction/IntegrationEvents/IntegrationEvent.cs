namespace Dialysis.DomainDrivenDesign.IntegrationEvents;

/// <summary>
/// Base for immutable integration events (cross-context, infrastructure-facing payloads).
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
}
