using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.HIS.Operations.Domain;

/// <summary>
/// Aggregate root: a member of facility staff with an optional primary role assignment.
/// </summary>
public sealed class StaffMember : AggregateRoot<Guid>
{
    public string DisplayName { get; private set; } = string.Empty;
    public string? PrimaryRoleCode { get; private set; }

    private StaffMember()
    {
    }

    private StaffMember(Guid id) : base(id)
    {
    }

    public static StaffMember Register(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("StaffMember DisplayName is required.");
        var normalized = displayName.Trim();
        if (normalized.Length > 256)
            throw new DomainException("StaffMember DisplayName must be 256 chars or fewer.");
        return new StaffMember(Guid.CreateVersion7())
        {
            DisplayName = normalized,
        };
    }

    public void AssignPrimaryRole(string roleCode)
    {
        if (string.IsNullOrWhiteSpace(roleCode))
            throw new DomainException("Role code is required to assign a primary role.");
        var normalized = roleCode.Trim();
        if (normalized.Length > 64)
            throw new DomainException("Role code must be 64 chars or fewer.");
        PrimaryRoleCode = normalized;
    }

    public void ClearPrimaryRole() => PrimaryRoleCode = null;
}
