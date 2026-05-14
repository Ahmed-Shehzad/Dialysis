namespace Dialysis.EHR.Core;

/// <summary>
/// Stable identifiers shared by every EHR sub-context (slice schemas, module slug,
/// outbox routing keys). Kept here so cross-slice code references one source of truth.
/// </summary>
public static class EhrModuleConstants
{
    public const string ModuleSlug = "ehr";

    public const string SchemaRegistration = "ehr_registration";

    public const string SchemaPatientChart = "ehr_patientchart";

    public const string SchemaScheduling = "ehr_scheduling";

    public const string SchemaPatientPortal = "ehr_patientportal";

    public const string SchemaClinicalNotes = "ehr_clinicalnotes";

    public const string SchemaBilling = "ehr_billing";

    public const string SchemaIntegration = "ehr_integration";
}
