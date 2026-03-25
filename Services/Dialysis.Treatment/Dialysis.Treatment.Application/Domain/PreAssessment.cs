using Dialysis.Treatment.Application.Domain.ValueObjects;

namespace Dialysis.Treatment.Application.Domain;

public sealed record PreAssessmentClinicalInput(
    decimal? PreWeightKg,
    int? BpSystolic,
    int? BpDiastolic,
    AccessType? AccessType,
    bool PrescriptionConfirmed,
    string? PainSymptomNotes,
    string? RecordedBy);

/// <summary>
/// Pre-treatment assessment recorded by clinician for a session.
/// </summary>
public sealed class PreAssessment
{
    public Ulid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public SessionId SessionId { get; private set; }
    public decimal? PreWeightKg { get; private set; }
    public int? BpSystolic { get; private set; }
    public int? BpDiastolic { get; private set; }
    public AccessType? AccessTypeValue { get; private set; }
    public bool PrescriptionConfirmed { get; private set; }
    public string? PainSymptomNotes { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public string? RecordedBy { get; private set; }

    private PreAssessment() { }

    public static PreAssessment Create(SessionId sessionId, string tenantId, PreAssessmentClinicalInput clinical)
    {
        return new PreAssessment
        {
            Id = Ulid.NewUlid(),
            TenantId = tenantId,
            SessionId = sessionId,
            PreWeightKg = clinical.PreWeightKg,
            BpSystolic = clinical.BpSystolic,
            BpDiastolic = clinical.BpDiastolic,
            AccessTypeValue = clinical.AccessType,
            PrescriptionConfirmed = clinical.PrescriptionConfirmed,
            PainSymptomNotes = clinical.PainSymptomNotes,
            RecordedAt = DateTimeOffset.UtcNow,
            RecordedBy = clinical.RecordedBy
        };
    }

    public void Update(PreAssessmentClinicalInput clinical)
    {
        PreWeightKg = clinical.PreWeightKg;
        BpSystolic = clinical.BpSystolic;
        BpDiastolic = clinical.BpDiastolic;
        AccessTypeValue = clinical.AccessType;
        PrescriptionConfirmed = clinical.PrescriptionConfirmed;
        PainSymptomNotes = clinical.PainSymptomNotes;
        RecordedBy = clinical.RecordedBy;
        RecordedAt = DateTimeOffset.UtcNow;
    }
}
