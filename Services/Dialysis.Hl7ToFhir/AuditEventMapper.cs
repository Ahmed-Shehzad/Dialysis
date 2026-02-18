using BuildingBlocks.Abstractions;

using Hl7.Fhir.Model;

namespace Dialysis.Hl7ToFhir;

/// <summary>
/// Maps <see cref="AuditRecordRequest"/> to FHIR R4 <see cref="AuditEvent"/>.
/// Aligns with IHE ATNA / DICOM audit model for C5 compliance.
/// </summary>
public static class AuditEventMapper
{
    private const string AuditEventTypeSystem = "http://terminology.hl7.org/CodeSystem/audit-event-type";

    /// <summary>
    /// Maps an audit record request to a FHIR AuditEvent resource.
    /// </summary>
    public static AuditEvent ToFhirAuditEvent(AuditRecordRequest request, string? sourceIdentifier = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var evt = new AuditEvent
        {
            Type = new Coding(AuditEventTypeSystem, "rest", "Restful Operation"),
            Action = MapAction(request.Action),
            Recorded = DateTimeOffset.UtcNow,
            Outcome = MapOutcome(request.Outcome),
            OutcomeDesc = request.Description,
            Agent =
            [
                new AuditEvent.AgentComponent
                {
                    Requestor = true,
                    AltId = request.UserId ?? "system",
                    Name = request.UserId ?? "System"
                }
            ],
            Source = new AuditEvent.SourceComponent
            {
                Site = request.TenantId ?? "default",
                Observer = new ResourceReference($"Device/{sourceIdentifier ?? "dialysis-pdms"}"),
                Type =
                [
                    new Coding("http://terminology.hl7.org/CodeSystem/audit-source-type", "4", "Application Server")
                ]
            }
        };

        if (!string.IsNullOrEmpty(request.ResourceType) || !string.IsNullOrEmpty(request.ResourceId))
            evt.Entity =
            [
                new AuditEvent.EntityComponent
                {
                    Type = new Coding("http://terminology.hl7.org/CodeSystem/audit-entity-type", "2", "System Object"),
                    Name = request.ResourceType,
                    Description = request.Description,
                    Detail = BuildEntityDetails(request)
                }
            ];

        return evt;
    }

    private static AuditEvent.AuditEventAction? MapAction(AuditAction action)
    {
        return action switch
        {
            AuditAction.Create => AuditEvent.AuditEventAction.C,
            AuditAction.Read => AuditEvent.AuditEventAction.R,
            AuditAction.Update => AuditEvent.AuditEventAction.U,
            AuditAction.Delete => AuditEvent.AuditEventAction.D,
            _ => AuditEvent.AuditEventAction.E
        };
    }

    private static AuditEvent.AuditEventOutcome MapOutcome(AuditOutcome outcome)
    {
        return outcome switch
        {
            AuditOutcome.Success => AuditEvent.AuditEventOutcome.N0,
            AuditOutcome.MinorFailure => AuditEvent.AuditEventOutcome.N4,
            AuditOutcome.SeriousFailure => AuditEvent.AuditEventOutcome.N8,
            AuditOutcome.MajorFailure => AuditEvent.AuditEventOutcome.N12,
            _ => AuditEvent.AuditEventOutcome.N0
        };
    }

    private static List<AuditEvent.DetailComponent> BuildEntityDetails(AuditRecordRequest request)
    {
        var details = new List<AuditEvent.DetailComponent>();

        if (!string.IsNullOrEmpty(request.ResourceType))
            details.Add(new AuditEvent.DetailComponent { Type = "ResourceType", Value = new FhirString(request.ResourceType) });

        if (!string.IsNullOrEmpty(request.ResourceId))
            details.Add(new AuditEvent.DetailComponent { Type = "ResourceId", Value = new FhirString(request.ResourceId) });

        if (!string.IsNullOrEmpty(request.TenantId))
            details.Add(new AuditEvent.DetailComponent { Type = "TenantId", Value = new FhirString(request.TenantId) });

        return details;
    }
}
