namespace BuildingBlocks.Tenancy;

/// <summary>
/// Provides the current tenant ID for the request. C5 multi-tenancy.
/// Resolved from X-Tenant-Id header; defaults to "default" when omitted.
/// </summary>
public interface ITenantContext
{
    string TenantId { get; }
}
