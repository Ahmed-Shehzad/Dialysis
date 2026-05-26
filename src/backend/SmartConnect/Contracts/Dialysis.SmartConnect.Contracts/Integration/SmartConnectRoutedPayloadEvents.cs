using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.SmartConnect.Contracts.Integration;

/// <summary>
/// SmartConnect dispatched a message to the Transponder bus as the outbound destination of a flow
/// route (adapter kind <c>transponder-bus</c>). Carries the routed bytes verbatim plus the operator-
/// supplied routing hint so consumers across modules can subscribe once and fan out by hint.
/// </summary>
/// <remarks>
/// Use this contract when a SmartConnect flow needs to publish onto the bus without committing to a
/// strongly-typed module-owned event. For module-specific contracts (e.g.
/// <see cref="DialysisMachineTreatmentSnapshotIntegrationEvent"/>) prefer the typed event and a
/// dedicated mapper inside SmartConnect.
/// </remarks>
public sealed record SmartConnectRoutedPayloadIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid FlowId,
    Guid IntegrationMessageId,
    int OutboundRouteOrdinal,
    string RoutingHint,
    string PayloadFormat,
    byte[] Payload,
    IReadOnlyDictionary<string, string> Headers) : IIntegrationEvent;
