namespace Dialysis.Identity.Provisioning.Domain;

public sealed class RoleAssignment
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public DateTime AssignedAtUtc { get; set; }
}
