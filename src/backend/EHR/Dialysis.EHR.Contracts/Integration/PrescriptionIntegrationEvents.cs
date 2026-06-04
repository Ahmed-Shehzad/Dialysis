using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.EHR.Contracts.Integration;

public sealed record PrescriptionOrderedIntegrationEvent : IIntegrationEvent
{
    public PrescriptionOrderedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid PrescriptionId,
        Guid PatientId,
        Guid EncounterId,
        Guid PrescribingProviderId,
        string MedicationRxnormCode,
        string MedicationDisplay,
        string DoseText,
        string FrequencyText,
        int QuantityDispensed,
        int RefillsAuthorized,
        string PharmacyNcpdpId,
        string TransmissionFormat)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.PrescriptionId = PrescriptionId;
        this.PatientId = PatientId;
        this.EncounterId = EncounterId;
        this.PrescribingProviderId = PrescribingProviderId;
        this.MedicationRxnormCode = MedicationRxnormCode;
        this.MedicationDisplay = MedicationDisplay;
        this.DoseText = DoseText;
        this.FrequencyText = FrequencyText;
        this.QuantityDispensed = QuantityDispensed;
        this.RefillsAuthorized = RefillsAuthorized;
        this.PharmacyNcpdpId = PharmacyNcpdpId;
        this.TransmissionFormat = TransmissionFormat;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid PrescriptionId { get; init; }
    public Guid PatientId { get; init; }
    public Guid EncounterId { get; init; }
    public Guid PrescribingProviderId { get; init; }
    public string MedicationRxnormCode { get; init; }
    public string MedicationDisplay { get; init; }
    public string DoseText { get; init; }
    public string FrequencyText { get; init; }
    public int QuantityDispensed { get; init; }
    public int RefillsAuthorized { get; init; }
    public string PharmacyNcpdpId { get; init; }
    public string TransmissionFormat { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid PrescriptionId, out Guid PatientId, out Guid EncounterId, out Guid PrescribingProviderId, out string MedicationRxnormCode, out string MedicationDisplay, out string DoseText, out string FrequencyText, out int QuantityDispensed, out int RefillsAuthorized, out string PharmacyNcpdpId, out string TransmissionFormat)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        PrescriptionId = this.PrescriptionId;
        PatientId = this.PatientId;
        EncounterId = this.EncounterId;
        PrescribingProviderId = this.PrescribingProviderId;
        MedicationRxnormCode = this.MedicationRxnormCode;
        MedicationDisplay = this.MedicationDisplay;
        DoseText = this.DoseText;
        FrequencyText = this.FrequencyText;
        QuantityDispensed = this.QuantityDispensed;
        RefillsAuthorized = this.RefillsAuthorized;
        PharmacyNcpdpId = this.PharmacyNcpdpId;
        TransmissionFormat = this.TransmissionFormat;
    }
}

public sealed record PrescriptionCancelledIntegrationEvent : IIntegrationEvent
{
    public PrescriptionCancelledIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid PrescriptionId,
        Guid PatientId,
        string ReasonCode)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.PrescriptionId = PrescriptionId;
        this.PatientId = PatientId;
        this.ReasonCode = ReasonCode;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid PrescriptionId { get; init; }
    public Guid PatientId { get; init; }
    public string ReasonCode { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid PrescriptionId, out Guid PatientId, out string ReasonCode)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        PrescriptionId = this.PrescriptionId;
        PatientId = this.PatientId;
        ReasonCode = this.ReasonCode;
    }
}

public sealed record PrescriptionAcknowledgedByPharmacyIntegrationEvent : IIntegrationEvent
{
    public PrescriptionAcknowledgedByPharmacyIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid PrescriptionId,
        string PharmacyNcpdpId,
        string AcknowledgementCode,
        string? Notes)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.PrescriptionId = PrescriptionId;
        this.PharmacyNcpdpId = PharmacyNcpdpId;
        this.AcknowledgementCode = AcknowledgementCode;
        this.Notes = Notes;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid PrescriptionId { get; init; }
    public string PharmacyNcpdpId { get; init; }
    public string AcknowledgementCode { get; init; }
    public string? Notes { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid PrescriptionId, out string PharmacyNcpdpId, out string AcknowledgementCode, out string? Notes)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        PrescriptionId = this.PrescriptionId;
        PharmacyNcpdpId = this.PharmacyNcpdpId;
        AcknowledgementCode = this.AcknowledgementCode;
        Notes = this.Notes;
    }
}
