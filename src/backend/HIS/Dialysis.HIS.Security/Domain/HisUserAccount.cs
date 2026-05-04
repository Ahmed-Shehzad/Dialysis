namespace Dialysis.HIS.Security.Domain;

public sealed class HisUserAccount
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<HisUserRole> UserRoles { get; set; } = new List<HisUserRole>();
}
