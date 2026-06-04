using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;

/// <summary>
/// Emitted when a patient is admitted to a ward in HIS patient-flow. Consumed by downstream care-coordination
/// and analytics sub-modules; EHR's encounter aggregate links the admission via PatientId + AdmittedAtUtc.
/// </summary>
public sealed record PatientAdmittedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Emitted when a patient is admitted to a ward in HIS patient-flow. Consumed by downstream care-coordination
    /// and analytics sub-modules; EHR's encounter aggregate links the admission via PatientId + AdmittedAtUtc.
    /// </summary>
    public PatientAdmittedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid AdmissionId,
        Guid PatientId,
        string WardCode,
        DateTime AdmittedAtUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.AdmissionId = AdmissionId;
        this.PatientId = PatientId;
        this.WardCode = WardCode;
        this.AdmittedAtUtc = AdmittedAtUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid AdmissionId { get; init; }
    public Guid PatientId { get; init; }
    public string WardCode { get; init; }
    public DateTime AdmittedAtUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid AdmissionId, out Guid PatientId, out string WardCode, out DateTime AdmittedAtUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        AdmissionId = this.AdmissionId;
        PatientId = this.PatientId;
        WardCode = this.WardCode;
        AdmittedAtUtc = this.AdmittedAtUtc;
    }
}
