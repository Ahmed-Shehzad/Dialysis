namespace Dialysis.DomainDrivenDesign.IntegrationEvents;

/// <summary>
/// A contract-first message published <strong>outside</strong> the bounded context (e.g. to a bus),
/// after the local domain transaction commits. Distinct from in-bounded-context <see cref="Dialysis.DomainDrivenDesign.DomainEvents.IDomainEvent"/>.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Stable id for deduplication and tracing.</summary>
    Guid EventId { get; }

    /// <summary>UTC time when the integration event was created (usually at publish boundary).</summary>
    DateTime OccurredOn { get; }
}
