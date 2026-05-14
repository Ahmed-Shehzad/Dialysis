using Dialysis.DomainDrivenDesign.DomainEvents;
using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.DomainDrivenDesign.Primitives;

/// <summary>
/// Consistency boundary that can raise <see cref="IDomainEvent"/> and <see cref="IIntegrationEvent"/> instances
/// for in-process handling and cross-context publication (respectively), after the aggregate’s invariants hold.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];
    private readonly List<IIntegrationEvent> _integrationEvents = [];

    protected AggregateRoot()
    {
    }

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    public IReadOnlyCollection<IIntegrationEvent> IntegrationEvents => _integrationEvents;

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Records an integration event to be published outside the bounded context (e.g. after commit / outbox).
    /// </summary>
    protected void RaiseIntegrationEvent(IIntegrationEvent integrationEvent) => _integrationEvents.Add(integrationEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public void ClearIntegrationEvents() => _integrationEvents.Clear();

    public override bool Equals(object? obj) => base.Equals(obj);

    public override int GetHashCode() => base.GetHashCode();

    public static bool operator ==(AggregateRoot<TId>? left, AggregateRoot<TId>? right) => Equals(left, right);

    public static bool operator !=(AggregateRoot<TId>? left, AggregateRoot<TId>? right) => !Equals(left, right);
}
