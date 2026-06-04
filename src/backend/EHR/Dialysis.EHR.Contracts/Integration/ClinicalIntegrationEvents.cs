using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.EHR.Contracts.Integration;

public sealed record EncounterOpenedIntegrationEvent : IIntegrationEvent
{
    public EncounterOpenedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid EncounterId,
        Guid PatientId,
        Guid ProviderId,
        string EncounterClassCode,
        DateTime StartedAtUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.EncounterId = EncounterId;
        this.PatientId = PatientId;
        this.ProviderId = ProviderId;
        this.EncounterClassCode = EncounterClassCode;
        this.StartedAtUtc = StartedAtUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid EncounterId { get; init; }
    public Guid PatientId { get; init; }
    public Guid ProviderId { get; init; }
    public string EncounterClassCode { get; init; }
    public DateTime StartedAtUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid EncounterId, out Guid PatientId, out Guid ProviderId, out string EncounterClassCode, out DateTime StartedAtUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        EncounterId = this.EncounterId;
        PatientId = this.PatientId;
        ProviderId = this.ProviderId;
        EncounterClassCode = this.EncounterClassCode;
        StartedAtUtc = this.StartedAtUtc;
    }
}

public sealed record EncounterClosedIntegrationEvent : IIntegrationEvent
{
    public EncounterClosedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid EncounterId,
        Guid PatientId,
        Guid ProviderId,
        DateTime ClosedAtUtc,
        IReadOnlyList<string> DiagnosisIcd10Codes,
        IReadOnlyList<string> ProcedureCptCodes)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.EncounterId = EncounterId;
        this.PatientId = PatientId;
        this.ProviderId = ProviderId;
        this.ClosedAtUtc = ClosedAtUtc;
        this.DiagnosisIcd10Codes = DiagnosisIcd10Codes;
        this.ProcedureCptCodes = ProcedureCptCodes;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid EncounterId { get; init; }
    public Guid PatientId { get; init; }
    public Guid ProviderId { get; init; }
    public DateTime ClosedAtUtc { get; init; }
    public IReadOnlyList<string> DiagnosisIcd10Codes { get; init; }
    public IReadOnlyList<string> ProcedureCptCodes { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid EncounterId, out Guid PatientId, out Guid ProviderId, out DateTime ClosedAtUtc, out IReadOnlyList<string> DiagnosisIcd10Codes, out IReadOnlyList<string> ProcedureCptCodes)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        EncounterId = this.EncounterId;
        PatientId = this.PatientId;
        ProviderId = this.ProviderId;
        ClosedAtUtc = this.ClosedAtUtc;
        DiagnosisIcd10Codes = this.DiagnosisIcd10Codes;
        ProcedureCptCodes = this.ProcedureCptCodes;
    }
}

public sealed record ClinicalNoteSignedIntegrationEvent : IIntegrationEvent
{
    public ClinicalNoteSignedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid NoteId,
        Guid EncounterId,
        Guid PatientId,
        Guid SignedByProviderId,
        DateTime SignedAtUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.NoteId = NoteId;
        this.EncounterId = EncounterId;
        this.PatientId = PatientId;
        this.SignedByProviderId = SignedByProviderId;
        this.SignedAtUtc = SignedAtUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid NoteId { get; init; }
    public Guid EncounterId { get; init; }
    public Guid PatientId { get; init; }
    public Guid SignedByProviderId { get; init; }
    public DateTime SignedAtUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid NoteId, out Guid EncounterId, out Guid PatientId, out Guid SignedByProviderId, out DateTime SignedAtUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        NoteId = this.NoteId;
        EncounterId = this.EncounterId;
        PatientId = this.PatientId;
        SignedByProviderId = this.SignedByProviderId;
        SignedAtUtc = this.SignedAtUtc;
    }
}
