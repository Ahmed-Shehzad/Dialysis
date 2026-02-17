using BuildingBlocks;

using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Domain.Entities;

/// <summary>
/// Clinical condition/diagnosis (e.g. ESRD, hypertension, diabetes). Maps to FHIR Condition.
/// </summary>
public sealed class Condition : BaseEntity
{
    public TenantId TenantId { get; private set; }
    public PatientId PatientId { get; private set; }
    public string CodeSystem { get; private set; } = "";  // e.g. http://hl7.org/fhir/sid/icd-10-cm, http://snomed.info/sct
    public string Code { get; private set; } = "";
    public string? Display { get; private set; }
    public string ClinicalStatus { get; private set; } = "active";  // active | recurrence | relapse | inactive | remission | resolved
    public string VerificationStatus { get; private set; } = "confirmed";  // unconfirmed | provisional | differential | confirmed | refuted
    public DateTime? OnsetDateTime { get; private set; }
    public DateTime? RecordedDate { get; private set; }

    private Condition()
    {
        TenantId = null!;
        PatientId = null!;
    }

    public static Condition Create(
        TenantId tenantId,
        PatientId patientId,
        string codeSystem,
        string code,
        string? display = null,
        string clinicalStatus = "active",
        string verificationStatus = "confirmed",
        DateTime? onset = null)
    {
        return new Condition
        {
            TenantId = tenantId,
            PatientId = patientId,
            CodeSystem = codeSystem,
            Code = code,
            Display = display ?? code,
            ClinicalStatus = clinicalStatus,
            VerificationStatus = verificationStatus,
            OnsetDateTime = onset,
            RecordedDate = DateTime.UtcNow
        };
    }

    public void Update(string? display, string? clinicalStatus, string? verificationStatus)
    {
        if (display is not null)
            Display = display;
        if (clinicalStatus is not null)
            ClinicalStatus = clinicalStatus;
        if (verificationStatus is not null)
            VerificationStatus = verificationStatus;
        ApplyUpdateDateTime();
    }
}
