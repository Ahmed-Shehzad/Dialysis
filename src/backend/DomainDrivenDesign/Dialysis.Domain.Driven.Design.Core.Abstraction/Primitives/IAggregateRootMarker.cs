using Dialysis.DomainDrivenDesign.DomainEvents;
using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.DomainDrivenDesign.Primitives;

/// <summary>
/// Non-generic surface of <see cref="AggregateRoot{TId}"/> for infrastructure that iterates over
/// heterogeneous aggregates (e.g. EF Core change-tracker scans) without knowing each aggregate's TId.
/// </summary>
public interface IAggregateRootMarker
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    IReadOnlyCollection<IIntegrationEvent> IntegrationEvents { get; }

    void ClearDomainEvents();

    void ClearIntegrationEvents();
}
