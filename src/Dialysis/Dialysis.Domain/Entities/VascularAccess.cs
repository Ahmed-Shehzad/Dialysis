using BuildingBlocks;

using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Domain.Entities;

/// <summary>
/// Vascular access for hemodialysis: fistula, graft, or catheter.
/// </summary>
public sealed class VascularAccess : BaseEntity
{
    public TenantId TenantId { get; private set; }
    public PatientId PatientId { get; private set; }
    public VascularAccessType Type { get; private set; }
    public string? Side { get; private set; }  // Left, Right
    public DateTime? PlacementDate { get; private set; }
    public VascularAccessStatus Status { get; private set; }
    public string? Notes { get; private set; }

    private VascularAccess()
    {
        TenantId = null!;
        PatientId = null!;
    }

    public static VascularAccess Create(
        TenantId tenantId,
        PatientId patientId,
        VascularAccessType type,
        string? side = null,
        DateTime? placementDate = null,
        string? notes = null)
    {
        return new VascularAccess
        {
            TenantId = tenantId,
            PatientId = patientId,
            Type = type,
            Side = side,
            PlacementDate = placementDate,
            Status = VascularAccessStatus.Active,
            Notes = notes
        };
    }

    public void UpdateStatus(VascularAccessStatus status, string? notes = null)
    {
        Status = status;
        if (notes is not null)
            Notes = notes;
        ApplyUpdateDateTime();
    }
}

public enum VascularAccessType
{
    Fistula = 0,
    Graft = 1,
    Catheter = 2
}

public enum VascularAccessStatus
{
    Active = 0,
    Complicated = 1,
    Failed = 2
}
