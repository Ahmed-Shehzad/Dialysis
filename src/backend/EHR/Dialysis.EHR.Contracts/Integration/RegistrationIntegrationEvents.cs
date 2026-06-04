using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.EHR.Contracts.Integration;

/// <summary>Emitted when a new patient is registered in the EHR (system of record for patient identity).</summary>
public sealed record PatientRegisteredIntegrationEvent : IIntegrationEvent
{
    /// <summary>Emitted when a new patient is registered in the EHR (system of record for patient identity).</summary>
    public PatientRegisteredIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid PatientId,
        string MedicalRecordNumber,
        string FamilyName,
        string GivenName,
        DateOnly DateOfBirth,
        string? SexAtBirthCode,
        string? PreferredLanguageCode)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.PatientId = PatientId;
        this.MedicalRecordNumber = MedicalRecordNumber;
        this.FamilyName = FamilyName;
        this.GivenName = GivenName;
        this.DateOfBirth = DateOfBirth;
        this.SexAtBirthCode = SexAtBirthCode;
        this.PreferredLanguageCode = PreferredLanguageCode;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid PatientId { get; init; }
    public string MedicalRecordNumber { get; init; }
    public string FamilyName { get; init; }
    public string GivenName { get; init; }
    public DateOnly DateOfBirth { get; init; }
    public string? SexAtBirthCode { get; init; }
    public string? PreferredLanguageCode { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid PatientId, out string MedicalRecordNumber, out string FamilyName, out string GivenName, out DateOnly DateOfBirth, out string? SexAtBirthCode, out string? PreferredLanguageCode)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        PatientId = this.PatientId;
        MedicalRecordNumber = this.MedicalRecordNumber;
        FamilyName = this.FamilyName;
        GivenName = this.GivenName;
        DateOfBirth = this.DateOfBirth;
        SexAtBirthCode = this.SexAtBirthCode;
        PreferredLanguageCode = this.PreferredLanguageCode;
    }
}

/// <summary>Emitted when patient demographics change in a way other modules should re-sync.</summary>
public sealed record PatientDemographicsUpdatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>Emitted when patient demographics change in a way other modules should re-sync.</summary>
    public PatientDemographicsUpdatedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid PatientId,
        string MedicalRecordNumber,
        string FamilyName,
        string GivenName)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.PatientId = PatientId;
        this.MedicalRecordNumber = MedicalRecordNumber;
        this.FamilyName = FamilyName;
        this.GivenName = GivenName;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid PatientId { get; init; }
    public string MedicalRecordNumber { get; init; }
    public string FamilyName { get; init; }
    public string GivenName { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid PatientId, out string MedicalRecordNumber, out string FamilyName, out string GivenName)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        PatientId = this.PatientId;
        MedicalRecordNumber = this.MedicalRecordNumber;
        FamilyName = this.FamilyName;
        GivenName = this.GivenName;
    }
}

/// <summary>Emitted when two patient records are merged (duplicate resolution); subscribers should re-target by surviving id.</summary>
public sealed record PatientsMergedIntegrationEvent : IIntegrationEvent
{
    /// <summary>Emitted when two patient records are merged (duplicate resolution); subscribers should re-target by surviving id.</summary>
    public PatientsMergedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid SurvivingPatientId,
        Guid SupersededPatientId,
        string SurvivingMedicalRecordNumber)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.SurvivingPatientId = SurvivingPatientId;
        this.SupersededPatientId = SupersededPatientId;
        this.SurvivingMedicalRecordNumber = SurvivingMedicalRecordNumber;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid SurvivingPatientId { get; init; }
    public Guid SupersededPatientId { get; init; }
    public string SurvivingMedicalRecordNumber { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid SurvivingPatientId, out Guid SupersededPatientId, out string SurvivingMedicalRecordNumber)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        SurvivingPatientId = this.SurvivingPatientId;
        SupersededPatientId = this.SupersededPatientId;
        SurvivingMedicalRecordNumber = this.SurvivingMedicalRecordNumber;
    }
}
