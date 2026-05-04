namespace Dialysis.HIS.Security.Domain;

public sealed class HisUserRole
{
    public Guid UserId { get; set; }

    public HisUserAccount User { get; set; } = null!;

    public Guid RoleId { get; set; }

    public HisRole Role { get; set; } = null!;
}
