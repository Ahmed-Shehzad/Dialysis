using BuildingBlocks.Abstractions;

namespace BuildingBlocks;

internal sealed class IntegrationEventBuffer : IIntegrationEventBuffer
{
    private readonly List<IIntegrationEvent> _events = [];

    public void Add(IIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        _events.Add(integrationEvent);
    }

    public IReadOnlyList<IIntegrationEvent> Drain()
    {
        var drained = _events.ToList();
        _events.Clear();
        return drained;
    }
}
