using BuildingBlocks;

using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Domain.Entities;

/// <summary>
/// Audit log entry for compliance. Records who did what to which resource and when.
/// </summary>
public sealed class AuditEvent : BaseEntity
{
    public TenantId TenantId { get; private set; }
    public string Actor { get; private set; } = "";
    public string Action { get; private set; } = "";
    public string ResourceType { get; private set; } = "";
    public string? ResourceId { get; private set; }
    public string? PatientId { get; private set; }
    public string? Details { get; private set; }

    private AuditEvent()
    {
        TenantId = null!;
    }

    public static AuditEvent Create(
        TenantId tenantId,
        string actor,
        string action,
        string resourceType,
        string? resourceId = null,
        string? patientId = null,
        string? details = null)
    {
        return new AuditEvent
        {
            TenantId = tenantId,
            Actor = actor ?? "system",
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            PatientId = patientId,
            Details = details
        };
    }
}
