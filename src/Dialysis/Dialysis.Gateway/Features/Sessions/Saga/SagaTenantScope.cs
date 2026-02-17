using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Gateway.Features.Sessions.Saga;

/// <summary>
/// Sets tenant context when saga runs outside HTTP (no X-Tenant-Id header).
/// </summary>
public static class SagaTenantScope
{
    private static readonly AsyncLocal<TenantId?> Current = new();

    public static void Set(TenantId tenantId) => Current.Value = tenantId;
    public static void Clear() => Current.Value = null;
    public static TenantId? Get() => Current.Value;
}
