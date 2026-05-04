namespace Dialysis.HIS.Security.Domain;

public sealed class HisRole
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}
