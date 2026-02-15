namespace Dialysis.Tenancy;

public sealed class TenantContext : ITenantContext
{
    public string? TenantId { get; set; }
    public bool IsResolved => !string.IsNullOrWhiteSpace(TenantId);
}
