namespace Dialysis.HIS.Operations.Domain;

public sealed class StaffMember
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? PrimaryRoleCode { get; set; }
}
