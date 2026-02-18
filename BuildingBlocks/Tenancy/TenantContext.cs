namespace BuildingBlocks.Tenancy;

/// <summary>
/// Scoped tenant context resolved from the current HTTP request.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    public const string DefaultTenantId = "default";
    public const string TenantIdHeader = "X-Tenant-Id";

    public string TenantId { get; set; } = DefaultTenantId;
}
