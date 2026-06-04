namespace Dialysis.BuildingBlocks.Hipaa.Audit;

/// <summary>
/// Marks an Intercessor command or query whose handler touches protected health information.
/// The <c>HipaaAuditingBehavior</c> emits a FHIR <c>AuditEvent</c> for every successful invocation
/// and a "minor failure" variant when the handler throws or returns a failed outcome.
///
/// Place on the request type, not the handler — the behaviour resolves it via attribute lookup
/// during pipeline construction so the audit decision is deterministic without runtime reflection
/// on every dispatch.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class PhiAccessAttribute : Attribute
{
    /// <summary>
    /// Marks an Intercessor command or query whose handler touches protected health information.
    /// The <c>HipaaAuditingBehavior</c> emits a FHIR <c>AuditEvent</c> for every successful invocation
    /// and a "minor failure" variant when the handler throws or returns a failed outcome.
    ///
    /// Place on the request type, not the handler — the behaviour resolves it via attribute lookup
    /// during pipeline construction so the audit decision is deterministic without runtime reflection
    /// on every dispatch.
    /// </summary>
    public PhiAccessAttribute(PhiAccessAction action, string fhirResourceType)
    {
        Action = action;
        FhirResourceType = fhirResourceType;
    }

    /// <summary>What the handler is doing with the PHI — feeds AuditEvent.action.</summary>
    public PhiAccessAction Action { get; }

    /// <summary>FHIR resource type the access concerns (e.g. <c>Patient</c>, <c>Encounter</c>). Surfaces on AuditEvent.entity.what.</summary>
    public string FhirResourceType { get; }
}

/// <summary>
/// Action verbs aligned to FHIR R4 <c>AuditEvent.AuditEventAction</c>. Mapped to the C/R/U/D/E codes
/// inside the auditing behaviour so consumers don't have to import the FHIR enum.
/// </summary>
public enum PhiAccessAction
{
    Read,
    Create,
    Update,
    Delete,
    Execute,
}
