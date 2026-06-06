using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Audit;

/// <summary>
/// Builds FHIR R4 <c>AuditEvent</c> resources per the standard audit-event profile. Action codes:
/// C(reate), R(ead), U(pdate), D(elete), E(xecute).
/// </summary>
public static class AuditEventBuilder
{
    /// <summary>
    /// CodeSystem the TEFCA permitted-purpose token is recorded under in <c>AuditEvent.purposeOfEvent</c>.
    /// TEFCA's permitted purposes are a distinct vocabulary from v3 PurposeOfUse, so we keep the token
    /// verbatim under a TEFCA system rather than lossily mapping it.
    /// </summary>
    public const string TefcaPurposeOfUseSystem = "https://rce.sequoiaproject.org/tefca/CodeSystem/permitted-purpose";

    public static AuditEvent Read(string resourceType, string resourceId, string? agentId, string moduleSlug, string? purposeOfUse = null)
        => Build("rest", "read", AuditEvent.AuditEventAction.R, resourceType, resourceId, agentId, moduleSlug, purposeOfUse);

    public static AuditEvent Search(string resourceType, string? agentId, string moduleSlug, string? purposeOfUse = null)
        => Build("rest", "search-type", AuditEvent.AuditEventAction.R, resourceType, resourceId: null, agentId, moduleSlug, purposeOfUse);

    public static AuditEvent Export(string jobId, string? agentId, string moduleSlug, string? purposeOfUse = null)
        => Build("rest", "$export", AuditEvent.AuditEventAction.E, "Bundle", jobId, agentId, moduleSlug, purposeOfUse);

    public static AuditEvent ConsentDenied(string resourceType, string? resourceId, string? agentId, string moduleSlug, string? purposeOfUse = null)
    {
        var auditEvent = Build("rest", "read", AuditEvent.AuditEventAction.R, resourceType, resourceId, agentId, moduleSlug, purposeOfUse);
        auditEvent.Outcome = AuditEvent.AuditEventOutcome.N4; // minor failure
        auditEvent.OutcomeDesc = "consent denied";
        return auditEvent;
    }

    private static AuditEvent Build(
        string typeSystemSlug,
        string subType,
        AuditEvent.AuditEventAction action,
        string resourceType,
        string? resourceId,
        string? agentId,
        string moduleSlug,
        string? purposeOfUse = null)
    {
        var auditEvent = new AuditEvent
        {
            Type = new Coding("http://terminology.hl7.org/CodeSystem/audit-event-type", typeSystemSlug),
            Subtype = [new Coding("http://hl7.org/fhir/restful-interaction", subType)],
            Action = action,
            Recorded = DateTimeOffset.UtcNow,
            Outcome = AuditEvent.AuditEventOutcome.N0,
            Source = new AuditEvent.SourceComponent
            {
                Site = moduleSlug,
                Observer = new ResourceReference($"Device/{moduleSlug}-host"),
            },
        };

        // The TEFCA permitted purpose under which the access was made (Art. 5(1)(a)/§630f-style
        // accountability — the audit log answers "why was this disclosed").
        if (!string.IsNullOrWhiteSpace(purposeOfUse))
        {
            auditEvent.PurposeOfEvent =
            [
                new CodeableConcept(TefcaPurposeOfUseSystem, purposeOfUse),
            ];
        }

        auditEvent.Agent.Add(new AuditEvent.AgentComponent
        {
            Requestor = true,
            Who = string.IsNullOrEmpty(agentId) ? null : new ResourceReference($"Practitioner/{agentId}"),
            PurposeOfUse = string.IsNullOrWhiteSpace(purposeOfUse)
                ? null
                : [new CodeableConcept(TefcaPurposeOfUseSystem, purposeOfUse)],
        });

        if (!string.IsNullOrEmpty(resourceId))
        {
            auditEvent.Entity.Add(new AuditEvent.EntityComponent
            {
                What = new ResourceReference($"{resourceType}/{resourceId}"),
            });
        }

        return auditEvent;
    }
}
