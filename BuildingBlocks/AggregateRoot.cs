using BuildingBlocks.Abstractions;

namespace BuildingBlocks;

public abstract class AggregateRoot : BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];
    private readonly List<IIntegrationEvent> _integrationEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public IReadOnlyCollection<IIntegrationEvent> IntegrationEvents => _integrationEvents.AsReadOnly();

    private void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    private void AddIntegrationEvent(IIntegrationEvent integrationEvent) => _integrationEvents.Add(integrationEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
    public void ClearIntegrationEvents() => _integrationEvents.Clear();

    protected void ApplyEvent(IEvent @event)
    {
        switch (@event)
        {
            case IDomainEvent domainEvent:
                AddDomainEvent(domainEvent);
                break;
            case IIntegrationEvent integrationEvent:
                AddIntegrationEvent(integrationEvent);
                break;
        }
    }
}
