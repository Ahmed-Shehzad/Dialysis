using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIE.Contracts.Integration;

/// <summary>
/// Emitted when an external partner POSTs a FHIR Patient resource that the HIE has accepted into its
/// MPI. EHR consumes this to enrich its own patient index without becoming directly coupled to FHIR.
/// </summary>
public sealed record ExternalPatientReferenceIngestedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Emitted when an external partner POSTs a FHIR Patient resource that the HIE has accepted into its
    /// MPI. EHR consumes this to enrich its own patient index without becoming directly coupled to FHIR.
    /// </summary>
    public ExternalPatientReferenceIngestedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        string PartnerId,
        string ExternalLogicalId,
        string? MedicalRecordNumber,
        string? FamilyName,
        string? GivenName,
        DateOnly? DateOfBirth,
        string? SexAtBirthCode)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.PartnerId = PartnerId;
        this.ExternalLogicalId = ExternalLogicalId;
        this.MedicalRecordNumber = MedicalRecordNumber;
        this.FamilyName = FamilyName;
        this.GivenName = GivenName;
        this.DateOfBirth = DateOfBirth;
        this.SexAtBirthCode = SexAtBirthCode;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public string PartnerId { get; init; }
    public string ExternalLogicalId { get; init; }
    public string? MedicalRecordNumber { get; init; }
    public string? FamilyName { get; init; }
    public string? GivenName { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? SexAtBirthCode { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out string PartnerId, out string ExternalLogicalId, out string? MedicalRecordNumber, out string? FamilyName, out string? GivenName, out DateOnly? DateOfBirth, out string? SexAtBirthCode)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        PartnerId = this.PartnerId;
        ExternalLogicalId = this.ExternalLogicalId;
        MedicalRecordNumber = this.MedicalRecordNumber;
        FamilyName = this.FamilyName;
        GivenName = this.GivenName;
        DateOfBirth = this.DateOfBirth;
        SexAtBirthCode = this.SexAtBirthCode;
    }
}

/// <summary>Emitted on accepted inbound FHIR <c>Encounter</c>.</summary>
public sealed record ExternalEncounterIngestedIntegrationEvent : IIntegrationEvent
{
    /// <summary>Emitted on accepted inbound FHIR <c>Encounter</c>.</summary>
    public ExternalEncounterIngestedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        string PartnerId,
        string ExternalLogicalId,
        string? PatientExternalLogicalId,
        DateTime? PeriodStartUtc,
        DateTime? PeriodEndUtc,
        string? ClassCode,
        string? ReasonCode)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.PartnerId = PartnerId;
        this.ExternalLogicalId = ExternalLogicalId;
        this.PatientExternalLogicalId = PatientExternalLogicalId;
        this.PeriodStartUtc = PeriodStartUtc;
        this.PeriodEndUtc = PeriodEndUtc;
        this.ClassCode = ClassCode;
        this.ReasonCode = ReasonCode;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public string PartnerId { get; init; }
    public string ExternalLogicalId { get; init; }
    public string? PatientExternalLogicalId { get; init; }
    public DateTime? PeriodStartUtc { get; init; }
    public DateTime? PeriodEndUtc { get; init; }
    public string? ClassCode { get; init; }
    public string? ReasonCode { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out string PartnerId, out string ExternalLogicalId, out string? PatientExternalLogicalId, out DateTime? PeriodStartUtc, out DateTime? PeriodEndUtc, out string? ClassCode, out string? ReasonCode)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        PartnerId = this.PartnerId;
        ExternalLogicalId = this.ExternalLogicalId;
        PatientExternalLogicalId = this.PatientExternalLogicalId;
        PeriodStartUtc = this.PeriodStartUtc;
        PeriodEndUtc = this.PeriodEndUtc;
        ClassCode = this.ClassCode;
        ReasonCode = this.ReasonCode;
    }
}

/// <summary>Emitted on accepted inbound FHIR <c>Observation</c>/<c>DiagnosticReport</c> with lab data.</summary>
public sealed record ExternalLabResultIngestedIntegrationEvent : IIntegrationEvent
{
    /// <summary>Emitted on accepted inbound FHIR <c>Observation</c>/<c>DiagnosticReport</c> with lab data.</summary>
    public ExternalLabResultIngestedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        string PartnerId,
        string ExternalLogicalId,
        string? PatientExternalLogicalId,
        string LoincCode,
        string DisplayName,
        string? ValueQuantity,
        string? Unit,
        DateTime? ObservedAtUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.PartnerId = PartnerId;
        this.ExternalLogicalId = ExternalLogicalId;
        this.PatientExternalLogicalId = PatientExternalLogicalId;
        this.LoincCode = LoincCode;
        this.DisplayName = DisplayName;
        this.ValueQuantity = ValueQuantity;
        this.Unit = Unit;
        this.ObservedAtUtc = ObservedAtUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public string PartnerId { get; init; }
    public string ExternalLogicalId { get; init; }
    public string? PatientExternalLogicalId { get; init; }
    public string LoincCode { get; init; }
    public string DisplayName { get; init; }
    public string? ValueQuantity { get; init; }
    public string? Unit { get; init; }
    public DateTime? ObservedAtUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out string PartnerId, out string ExternalLogicalId, out string? PatientExternalLogicalId, out string LoincCode, out string DisplayName, out string? ValueQuantity, out string? Unit, out DateTime? ObservedAtUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        PartnerId = this.PartnerId;
        ExternalLogicalId = this.ExternalLogicalId;
        PatientExternalLogicalId = this.PatientExternalLogicalId;
        LoincCode = this.LoincCode;
        DisplayName = this.DisplayName;
        ValueQuantity = this.ValueQuantity;
        Unit = this.Unit;
        ObservedAtUtc = this.ObservedAtUtc;
    }
}

/// <summary>Emitted on accepted inbound FHIR <c>Procedure</c> describing a dialysis session.</summary>
public sealed record ExternalDialysisSessionIngestedIntegrationEvent : IIntegrationEvent
{
    /// <summary>Emitted on accepted inbound FHIR <c>Procedure</c> describing a dialysis session.</summary>
    public ExternalDialysisSessionIngestedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        string PartnerId,
        string ExternalLogicalId,
        string? PatientExternalLogicalId,
        DateTime? PerformedStartUtc,
        DateTime? PerformedEndUtc,
        string? OutcomeCode)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.PartnerId = PartnerId;
        this.ExternalLogicalId = ExternalLogicalId;
        this.PatientExternalLogicalId = PatientExternalLogicalId;
        this.PerformedStartUtc = PerformedStartUtc;
        this.PerformedEndUtc = PerformedEndUtc;
        this.OutcomeCode = OutcomeCode;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public string PartnerId { get; init; }
    public string ExternalLogicalId { get; init; }
    public string? PatientExternalLogicalId { get; init; }
    public DateTime? PerformedStartUtc { get; init; }
    public DateTime? PerformedEndUtc { get; init; }
    public string? OutcomeCode { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out string PartnerId, out string ExternalLogicalId, out string? PatientExternalLogicalId, out DateTime? PerformedStartUtc, out DateTime? PerformedEndUtc, out string? OutcomeCode)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        PartnerId = this.PartnerId;
        ExternalLogicalId = this.ExternalLogicalId;
        PatientExternalLogicalId = this.PatientExternalLogicalId;
        PerformedStartUtc = this.PerformedStartUtc;
        PerformedEndUtc = this.PerformedEndUtc;
        OutcomeCode = this.OutcomeCode;
    }
}
