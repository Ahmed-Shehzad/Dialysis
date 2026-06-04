using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.PDMS.Contracts.Integration;

/// <summary>
/// Published when a completed dialysis session is ready to be billed. EHR.Billing consumes
/// this and creates a <c>Charge</c> with the appropriate CPT code (90935 / 90937 HD,
/// 90945 / 90947 PD). The CPT mapping is resolved by the producer so EHR.Billing never has
/// to model PDMS modality semantics.
/// </summary>
public sealed record DialysisSessionChargeReadyIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Published when a completed dialysis session is ready to be billed. EHR.Billing consumes
    /// this and creates a <c>Charge</c> with the appropriate CPT code (90935 / 90937 HD,
    /// 90945 / 90947 PD). The CPT mapping is resolved by the producer so EHR.Billing never has
    /// to model PDMS modality semantics.
    /// </summary>
    public DialysisSessionChargeReadyIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid SessionId,
        Guid PatientId,
        string Modality,
        int DurationMinutes,
        DateTime CompletedAtUtc,
        string CptCode)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.SessionId = SessionId;
        this.PatientId = PatientId;
        this.Modality = Modality;
        this.DurationMinutes = DurationMinutes;
        this.CompletedAtUtc = CompletedAtUtc;
        this.CptCode = CptCode;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid SessionId { get; init; }
    public Guid PatientId { get; init; }
    public string Modality { get; init; }
    public int DurationMinutes { get; init; }
    public DateTime CompletedAtUtc { get; init; }
    public string CptCode { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid SessionId, out Guid PatientId, out string Modality, out int DurationMinutes, out DateTime CompletedAtUtc, out string CptCode)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        SessionId = this.SessionId;
        PatientId = this.PatientId;
        Modality = this.Modality;
        DurationMinutes = this.DurationMinutes;
        CompletedAtUtc = this.CompletedAtUtc;
        CptCode = this.CptCode;
    }
}
