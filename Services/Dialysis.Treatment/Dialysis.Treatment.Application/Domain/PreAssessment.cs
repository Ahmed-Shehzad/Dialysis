using Dialysis.Treatment.Application.Domain.ValueObjects;

namespace Dialysis.Treatment.Application.Domain;

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

    public static PreAssessment Create(
        SessionId sessionId,
        string tenantId,
        decimal? preWeightKg,
        int? bpSystolic,
        int? bpDiastolic,
        AccessType? accessType,
        bool prescriptionConfirmed,
        string? painSymptomNotes,
        string? recordedBy)
    {
        return new PreAssessment
        {
            Id = Ulid.NewUlid(),
            TenantId = tenantId,
            SessionId = sessionId,
            PreWeightKg = preWeightKg,
            BpSystolic = bpSystolic,
            BpDiastolic = bpDiastolic,
            AccessTypeValue = accessType,
            PrescriptionConfirmed = prescriptionConfirmed,
            PainSymptomNotes = painSymptomNotes,
            RecordedAt = DateTimeOffset.UtcNow,
            RecordedBy = recordedBy
        };
    }

    public void Update(
        decimal? preWeightKg,
        int? bpSystolic,
        int? bpDiastolic,
        AccessType? accessType,
        bool prescriptionConfirmed,
        string? painSymptomNotes,
        string? recordedBy)
    {
        PreWeightKg = preWeightKg;
        BpSystolic = bpSystolic;
        BpDiastolic = bpDiastolic;
        AccessTypeValue = accessType;
        PrescriptionConfirmed = prescriptionConfirmed;
        PainSymptomNotes = painSymptomNotes;
        RecordedBy = recordedBy;
        RecordedAt = DateTimeOffset.UtcNow;
    }
}
