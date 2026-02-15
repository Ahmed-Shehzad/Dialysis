namespace Dialysis.Tenancy;

/// <summary>Provides the current tenant identifier for the request scope.</summary>
public interface ITenantContext
{
    string? TenantId { get; }
    bool IsResolved { get; }
}
