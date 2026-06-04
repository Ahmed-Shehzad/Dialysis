using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.EHR.Contracts.Integration;

public sealed record LabOrderPlacedIntegrationEvent : IIntegrationEvent
{
    public LabOrderPlacedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid LabOrderId,
        Guid PatientId,
        Guid EncounterId,
        Guid OrderingProviderId,
        string LabFacilityCode,
        IReadOnlyList<string> LoincPanelCodes,
        string TransmissionFormat)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.LabOrderId = LabOrderId;
        this.PatientId = PatientId;
        this.EncounterId = EncounterId;
        this.OrderingProviderId = OrderingProviderId;
        this.LabFacilityCode = LabFacilityCode;
        this.LoincPanelCodes = LoincPanelCodes;
        this.TransmissionFormat = TransmissionFormat;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid LabOrderId { get; init; }
    public Guid PatientId { get; init; }
    public Guid EncounterId { get; init; }
    public Guid OrderingProviderId { get; init; }
    public string LabFacilityCode { get; init; }
    public IReadOnlyList<string> LoincPanelCodes { get; init; }
    public string TransmissionFormat { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid LabOrderId, out Guid PatientId, out Guid EncounterId, out Guid OrderingProviderId, out string LabFacilityCode, out IReadOnlyList<string> LoincPanelCodes, out string TransmissionFormat)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        LabOrderId = this.LabOrderId;
        PatientId = this.PatientId;
        EncounterId = this.EncounterId;
        OrderingProviderId = this.OrderingProviderId;
        LabFacilityCode = this.LabFacilityCode;
        LoincPanelCodes = this.LoincPanelCodes;
        TransmissionFormat = this.TransmissionFormat;
    }
}

public sealed record LabOrderCancelledIntegrationEvent : IIntegrationEvent
{
    public LabOrderCancelledIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid LabOrderId,
        Guid PatientId,
        string ReasonCode)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.LabOrderId = LabOrderId;
        this.PatientId = PatientId;
        this.ReasonCode = ReasonCode;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid LabOrderId { get; init; }
    public Guid PatientId { get; init; }
    public string ReasonCode { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid LabOrderId, out Guid PatientId, out string ReasonCode)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        LabOrderId = this.LabOrderId;
        PatientId = this.PatientId;
        ReasonCode = this.ReasonCode;
    }
}

public sealed record LabResultReceivedIntegrationEvent : IIntegrationEvent
{
    public LabResultReceivedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid LabResultId,
        Guid LabOrderId,
        Guid PatientId,
        string LoincCode,
        string ValueText,
        string? UnitCode,
        string? ReferenceRangeText,
        string AbnormalFlag,
        DateTime ObservedAtUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.LabResultId = LabResultId;
        this.LabOrderId = LabOrderId;
        this.PatientId = PatientId;
        this.LoincCode = LoincCode;
        this.ValueText = ValueText;
        this.UnitCode = UnitCode;
        this.ReferenceRangeText = ReferenceRangeText;
        this.AbnormalFlag = AbnormalFlag;
        this.ObservedAtUtc = ObservedAtUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid LabResultId { get; init; }
    public Guid LabOrderId { get; init; }
    public Guid PatientId { get; init; }
    public string LoincCode { get; init; }
    public string ValueText { get; init; }
    public string? UnitCode { get; init; }
    public string? ReferenceRangeText { get; init; }
    public string AbnormalFlag { get; init; }
    public DateTime ObservedAtUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid LabResultId, out Guid LabOrderId, out Guid PatientId, out string LoincCode, out string ValueText, out string? UnitCode, out string? ReferenceRangeText, out string AbnormalFlag, out DateTime ObservedAtUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        LabResultId = this.LabResultId;
        LabOrderId = this.LabOrderId;
        PatientId = this.PatientId;
        LoincCode = this.LoincCode;
        ValueText = this.ValueText;
        UnitCode = this.UnitCode;
        ReferenceRangeText = this.ReferenceRangeText;
        AbnormalFlag = this.AbnormalFlag;
        ObservedAtUtc = this.ObservedAtUtc;
    }
}
