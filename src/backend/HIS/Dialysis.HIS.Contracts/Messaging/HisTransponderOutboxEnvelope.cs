using System.Text.Json;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.Messaging;

/// <summary>
/// Maps HIS <see cref="IIntegrationEvent"/> instances to <see cref="TransponderOutboxEnvelope"/> for the shared Transponder transactional outbox.
/// </summary>
public static class HisTransponderOutboxEnvelope
{
    private static readonly JsonSerializerOptions _jsonOptions = new();

    public static TransponderOutboxEnvelope From(IIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var type = integrationEvent.GetType();
        var json = JsonSerializer.Serialize(integrationEvent, type, _jsonOptions);
        var aq = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
        return new TransponderOutboxEnvelope(aq, json, Id: integrationEvent.EventId);
    }
}
