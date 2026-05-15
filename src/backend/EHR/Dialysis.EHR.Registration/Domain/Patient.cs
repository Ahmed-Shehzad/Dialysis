using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.EHR.Registration.Domain;

public enum PatientStatus
{
    Active = 1,
    Inactive = 2,
    Deceased = 3,
    Merged = 4,
}

/// <summary>
/// EHR system-of-record patient aggregate. Owns identity (MRN), demographics, contact info,
/// preferred-language, and lifecycle state. Clinical data lives in the PatientChart bounded context
/// keyed by <see cref="AggregateRoot{TId}.Id"/>.
/// </summary>
public sealed class Patient : AggregateRoot<Guid>
{
    private readonly List<ContactPoint> _contactPoints = new();

    private Patient()
    {
    }

    public Patient(Guid id) : base(id)
    {
    }

    public string MedicalRecordNumber { get; private set; } = string.Empty;

    public HumanName Name { get; private set; } = null!;

    public DateOnly DateOfBirth { get; private set; }

    public string? SexAtBirthCode { get; private set; }

    public string? PreferredLanguageCode { get; private set; }

    public PostalAddress? PrimaryAddress { get; private set; }

    public IReadOnlyCollection<ContactPoint> ContactPoints => _contactPoints;

    public PatientStatus Status { get; private set; } = PatientStatus.Active;

    public Guid? SupersededByPatientId { get; private set; }

    /// <summary>
    /// Tracks the last time this aggregate's persisted state changed. Surfaces as the FHIR
    /// <c>Meta.lastUpdated</c> on the projected Patient resource and powers the bulk-export
    /// <c>_since</c> filter so external pipelines pull only what changed since the last sync.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    public static Patient Register(
        Guid id,
        string medicalRecordNumber,
        HumanName name,
        DateOnly dateOfBirth,
        string? sexAtBirthCode,
        string? preferredLanguageCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(medicalRecordNumber);
        ArgumentNullException.ThrowIfNull(name);

        var patient = new Patient(id)
        {
            MedicalRecordNumber = medicalRecordNumber.Trim(),
            Name = name,
            DateOfBirth = dateOfBirth,
            SexAtBirthCode = sexAtBirthCode?.Trim(),
            PreferredLanguageCode = preferredLanguageCode?.Trim(),
        };

        patient.RaiseIntegrationEvent(new PatientRegisteredIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            PatientId: patient.Id,
            MedicalRecordNumber: patient.MedicalRecordNumber,
            FamilyName: name.FamilyName,
            GivenName: name.GivenName,
            DateOfBirth: dateOfBirth,
            SexAtBirthCode: patient.SexAtBirthCode,
            PreferredLanguageCode: patient.PreferredLanguageCode));

        return patient;
    }

    public void UpdateDemographics(HumanName name, string? sexAtBirthCode, string? preferredLanguageCode, PostalAddress? primaryAddress)
    {
        ArgumentNullException.ThrowIfNull(name);
        EnsureMutable();
        Name = name;
        SexAtBirthCode = sexAtBirthCode?.Trim();
        PreferredLanguageCode = preferredLanguageCode?.Trim();
        PrimaryAddress = primaryAddress;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        RaiseIntegrationEvent(new PatientDemographicsUpdatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            PatientId: Id,
            MedicalRecordNumber: MedicalRecordNumber,
            FamilyName: name.FamilyName,
            GivenName: name.GivenName));
    }

    public void SetContactPoints(IEnumerable<ContactPoint> contactPoints)
    {
        ArgumentNullException.ThrowIfNull(contactPoints);
        EnsureMutable();
        _contactPoints.Clear();
        _contactPoints.AddRange(contactPoints);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MergeInto(Guid survivingPatientId, string survivingMedicalRecordNumber)
    {
        if (Status == PatientStatus.Merged)
            throw new InvalidOperationException("Patient is already merged.");
        if (Id == survivingPatientId)
            throw new InvalidOperationException("Cannot merge a patient into itself.");

        Status = PatientStatus.Merged;
        SupersededByPatientId = survivingPatientId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        RaiseIntegrationEvent(new PatientsMergedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            SurvivingPatientId: survivingPatientId,
            SupersededPatientId: Id,
            SurvivingMedicalRecordNumber: survivingMedicalRecordNumber));
    }

    private void EnsureMutable()
    {
        if (Status == PatientStatus.Merged)
            throw new InvalidOperationException("Cannot modify a merged patient. Operate on the surviving record.");
    }
}
