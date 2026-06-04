namespace Dialysis.BuildingBlocks.Fhir.AspNetCore.Audit;

/// <summary>
/// Marks a controller action as touching personally-identifiable health information.
/// The <see cref="PhiAccessAuditFilter"/> writes a FHIR <c>AuditEvent</c> row for every
/// matched request — recording the operator sub, the patient id (resolved from the
/// declared route key), the activity name, and the response status. This closes the
/// audit-trail loop the DPIA cites for GDPR Art. 30 + BDSG §22 + the HIPAA security rule.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class PhiAccessAttribute : Attribute
{
    /// <summary>
    /// Marks a controller action as touching personally-identifiable health information.
    /// The <see cref="PhiAccessAuditFilter"/> writes a FHIR <c>AuditEvent</c> row for every
    /// matched request — recording the operator sub, the patient id (resolved from the
    /// declared route key), the activity name, and the response status. This closes the
    /// audit-trail loop the DPIA cites for GDPR Art. 30 + BDSG §22 + the HIPAA security rule.
    /// </summary>
    public PhiAccessAttribute(string activityName) => ActivityName = activityName;

    /// <summary>
    /// Stable activity identifier — matches the processing-activity name registered with
    /// <c>services.AddEuDataProtection("module-slug", registry =&gt; registry.RegisterActivity("…"))</c>.
    /// Example: <c>"pdms.medications.administer"</c>.
    /// </summary>
    public string ActivityName { get; }

    /// <summary>
    /// Route value name carrying the patient identifier — when set, the filter pulls the
    /// patient id from <c>HttpContext.Request.RouteValues[PatientIdRouteKey]</c>. Leave
    /// unset to read it from <c>"patientId"</c> by default.
    /// </summary>
    public string PatientIdRouteKey { get; init; } = "patientId";

    /// <summary>
    /// Route value name carrying a session / encounter id — when set, the filter pulls it
    /// and includes it on the AuditEvent's entity sub-list so audit consumers can join
    /// the row to the matching aggregate without a downstream lookup.
    /// </summary>
    public string? SessionIdRouteKey { get; init; }
}
