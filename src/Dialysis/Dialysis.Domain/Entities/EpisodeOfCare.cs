using BuildingBlocks;

using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Domain.Entities;

/// <summary>
/// Longitudinal episode of care (e.g. chronic dialysis treatment). Maps to FHIR EpisodeOfCare.
/// </summary>
public sealed class EpisodeOfCare : BaseEntity
{
    public TenantId TenantId { get; private set; }
    public PatientId PatientId { get; private set; }
    public string Status { get; private set; } = "active";  // planned | waitlist | active | onhold | finished | cancelled
    public DateTime? PeriodStart { get; private set; }
    public DateTime? PeriodEnd { get; private set; }
    public string? Description { get; private set; }
    public List<string> DiagnosisConditionIds { get; private set; } = [];  // Condition Ids referenced for FHIR diagnosis

    private EpisodeOfCare()
    {
        TenantId = null!;
        PatientId = null!;
    }

    public static EpisodeOfCare Create(
        TenantId tenantId,
        PatientId patientId,
        string status = "active",
        DateTime? periodStart = null,
        DateTime? periodEnd = null,
        string? description = null,
        IReadOnlyList<string>? diagnosisConditionIds = null)
    {
        return new EpisodeOfCare
        {
            TenantId = tenantId,
            PatientId = patientId,
            Status = status,
            PeriodStart = periodStart ?? DateTime.UtcNow.Date,
            PeriodEnd = periodEnd,
            Description = description ?? "Chronic dialysis care",
            DiagnosisConditionIds = diagnosisConditionIds?.ToList() ?? []
        };
    }

    public void Update(string? status, DateTime? periodEnd, string? description, IReadOnlyList<string>? diagnosisConditionIds)
    {
        if (status is not null)
            Status = status;
        if (periodEnd.HasValue)
            PeriodEnd = periodEnd;
        if (description is not null)
            Description = description;
        if (diagnosisConditionIds is not null)
            DiagnosisConditionIds = diagnosisConditionIds.ToList();
        ApplyUpdateDateTime();
    }
}
