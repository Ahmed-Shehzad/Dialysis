using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;

/// <summary>
/// Emitted when an admitted patient is discharged in HIS patient-flow. Consumed by care-coordination
/// sub-modules to prompt proactive post-discharge follow-up (EHR links it via PatientId + DischargedAtUtc).
/// </summary>
public sealed record PatientDischargedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Emitted when an admitted patient is discharged in HIS patient-flow. Consumed by care-coordination
    /// sub-modules to prompt proactive post-discharge follow-up (EHR links it via PatientId + DischargedAtUtc).
    /// </summary>
    public PatientDischargedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid AdmissionId,
        Guid PatientId,
        string WardCode,
        DateTime DischargedAtUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.AdmissionId = AdmissionId;
        this.PatientId = PatientId;
        this.WardCode = WardCode;
        this.DischargedAtUtc = DischargedAtUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid AdmissionId { get; init; }
    public Guid PatientId { get; init; }
    public string WardCode { get; init; }
    public DateTime DischargedAtUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid AdmissionId, out Guid PatientId, out string WardCode, out DateTime DischargedAtUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        AdmissionId = this.AdmissionId;
        PatientId = this.PatientId;
        WardCode = this.WardCode;
        DischargedAtUtc = this.DischargedAtUtc;
    }
}
