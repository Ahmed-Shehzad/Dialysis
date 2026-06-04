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
public sealed record SmartConnectRoutedPayloadIntegrationEvent : IIntegrationEvent
{
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
    public SmartConnectRoutedPayloadIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid FlowId,
        Guid IntegrationMessageId,
        int OutboundRouteOrdinal,
        string RoutingHint,
        string PayloadFormat,
        byte[] Payload,
        IReadOnlyDictionary<string, string> Headers)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.FlowId = FlowId;
        this.IntegrationMessageId = IntegrationMessageId;
        this.OutboundRouteOrdinal = OutboundRouteOrdinal;
        this.RoutingHint = RoutingHint;
        this.PayloadFormat = PayloadFormat;
        this.Payload = Payload;
        this.Headers = Headers;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid FlowId { get; init; }
    public Guid IntegrationMessageId { get; init; }
    public int OutboundRouteOrdinal { get; init; }
    public string RoutingHint { get; init; }
    public string PayloadFormat { get; init; }
    public byte[] Payload { get; init; }
    public IReadOnlyDictionary<string, string> Headers { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid FlowId, out Guid IntegrationMessageId, out int OutboundRouteOrdinal, out string RoutingHint, out string PayloadFormat, out byte[] Payload, out IReadOnlyDictionary<string, string> Headers)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        FlowId = this.FlowId;
        IntegrationMessageId = this.IntegrationMessageId;
        OutboundRouteOrdinal = this.OutboundRouteOrdinal;
        RoutingHint = this.RoutingHint;
        PayloadFormat = this.PayloadFormat;
        Payload = this.Payload;
        Headers = this.Headers;
    }
}
