using System.Text.Json;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.Simulation.Contracts.Messaging;

/// <summary>
/// Maps Simulation <see cref="IIntegrationEvent"/> instances to <see cref="TransponderOutboxEnvelope"/>
/// for the shared Transponder transactional outbox.
/// </summary>
public static class SimulationTransponderOutboxEnvelope
{
    private static readonly JsonSerializerOptions _jsonOptions = new();

    /// <summary>Serializes <paramref name="integrationEvent"/> into a transactional-outbox envelope.</summary>
    public static TransponderOutboxEnvelope From(IIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var type = integrationEvent.GetType();
        var json = JsonSerializer.Serialize((object)integrationEvent, type, _jsonOptions);
        var aq = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
        return new TransponderOutboxEnvelope(aq, json, Id: integrationEvent.EventId);
    }
}
