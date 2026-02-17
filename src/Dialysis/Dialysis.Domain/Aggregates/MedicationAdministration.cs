using BuildingBlocks;

using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Domain.Aggregates;

/// <summary>
/// Domain aggregate for medication administration during dialysis (ESA, iron, heparin, binders).
/// </summary>
public sealed class MedicationAdministration : AggregateRoot
{
    public TenantId TenantId { get; private set; }
    public PatientId PatientId { get; private set; }
    public string? SessionId { get; private set; }
    public string MedicationCode { get; private set; } = "";  // RxNorm, SNOMED, or local code
    public string? MedicationDisplay { get; private set; }
    public string? DoseQuantity { get; private set; }
    public string? DoseUnit { get; private set; }
    public string? Route { get; private set; }  // IV, PO, etc.
    public DateTimeOffset EffectiveAt { get; private set; }
    public string? Status { get; private set; }  // completed, in-progress, etc.
    public string? ReasonText { get; private set; }
    public string? PerformerId { get; private set; }

    private MedicationAdministration()
    {
        TenantId = null!;
        PatientId = null!;
    }

    public static MedicationAdministration Create(
        TenantId tenantId,
        PatientId patientId,
        string medicationCode,
        string? medicationDisplay,
        string? doseQuantity,
        string? doseUnit,
        string? route,
        DateTimeOffset effectiveAt,
        string? sessionId = null,
        string? reasonText = null,
        string? performerId = null)
    {
        return new MedicationAdministration
        {
            TenantId = tenantId,
            PatientId = patientId,
            SessionId = sessionId,
            MedicationCode = medicationCode ?? throw new ArgumentNullException(nameof(medicationCode)),
            MedicationDisplay = medicationDisplay,
            DoseQuantity = doseQuantity,
            DoseUnit = doseUnit,
            Route = route,
            EffectiveAt = effectiveAt,
            Status = "completed",
            ReasonText = reasonText,
            PerformerId = performerId
        };
    }
}
