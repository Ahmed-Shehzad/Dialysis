using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Audit;

/// <summary>
/// Builds FHIR R4 <c>AuditEvent</c> resources per the standard audit-event profile. Action codes:
/// C(reate), R(ead), U(pdate), D(elete), E(xecute).
/// </summary>
public static class AuditEventBuilder
{
    public static AuditEvent Read(string resourceType, string resourceId, string? agentId, string moduleSlug)
        => Build("rest", "read", AuditEvent.AuditEventAction.R, resourceType, resourceId, agentId, moduleSlug);

    public static AuditEvent Search(string resourceType, string? agentId, string moduleSlug)
        => Build("rest", "search-type", AuditEvent.AuditEventAction.R, resourceType, resourceId: null, agentId, moduleSlug);

    public static AuditEvent Export(string jobId, string? agentId, string moduleSlug)
        => Build("rest", "$export", AuditEvent.AuditEventAction.E, "Bundle", jobId, agentId, moduleSlug);

    public static AuditEvent ConsentDenied(string resourceType, string? resourceId, string? agentId, string moduleSlug)
    {
        var auditEvent = Build("rest", "read", AuditEvent.AuditEventAction.R, resourceType, resourceId, agentId, moduleSlug);
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
        string moduleSlug)
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

        auditEvent.Agent.Add(new AuditEvent.AgentComponent
        {
            Requestor = true,
            Who = string.IsNullOrEmpty(agentId) ? null : new ResourceReference($"Practitioner/{agentId}"),
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
