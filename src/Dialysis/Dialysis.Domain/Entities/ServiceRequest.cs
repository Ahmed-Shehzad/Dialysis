using BuildingBlocks;

using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Domain.Entities;

/// <summary>
/// Domain entity for orders (ServiceRequest) - dialysis orders, medication orders, etc.
/// </summary>
public sealed class ServiceRequest : BaseEntity
{
    public TenantId TenantId { get; private set; }
    public PatientId PatientId { get; private set; }
    public string Code { get; private set; } = "";
    public string? Display { get; private set; }
    public string Status { get; private set; } = "active";  // active, completed, cancelled
    public string? Intent { get; private set; }  // order, plan
    public string? EncounterId { get; private set; }
    public string? SessionId { get; private set; }
    public DateTimeOffset? AuthoredOn { get; private set; }
    public string? ReasonText { get; private set; }
    public string? RequesterId { get; private set; }
    public string? Frequency { get; private set; }  // e.g. "3x per week"
    public string? Category { get; private set; }  // dialysis, medication, lab

    private ServiceRequest()
    {
        TenantId = null!;
        PatientId = null!;
    }

    public static ServiceRequest Create(
        TenantId tenantId,
        PatientId patientId,
        string code,
        string? display,
        string? intent = "order",
        string? encounterId = null,
        string? sessionId = null,
        DateTimeOffset? authoredOn = null,
        string? reasonText = null,
        string? requesterId = null,
        string? frequency = null,
        string? category = null)
    {
        return new ServiceRequest
        {
            TenantId = tenantId,
            PatientId = patientId,
            Code = code ?? throw new ArgumentNullException(nameof(code)),
            Display = display,
            Status = "active",
            Intent = intent ?? "order",
            EncounterId = encounterId,
            SessionId = sessionId,
            AuthoredOn = authoredOn ?? DateTimeOffset.UtcNow,
            ReasonText = reasonText,
            RequesterId = requesterId,
            Frequency = frequency,
            Category = category
        };
    }

    public void Complete()
    {
        Status = "completed";
        ApplyUpdateDateTime();
    }

    public void Cancel()
    {
        Status = "cancelled";
        ApplyUpdateDateTime();
    }
}
