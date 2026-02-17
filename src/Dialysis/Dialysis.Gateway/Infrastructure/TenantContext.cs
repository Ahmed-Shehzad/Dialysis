using Dialysis.Gateway.Features.Sessions.Saga;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Gateway.Infrastructure;

/// <summary>
/// Resolves tenant from HTTP context or saga scope. Implements ITenantContext for DeviceIngestion.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public TenantId TenantId
    {
        get
        {
            if (SagaTenantScope.Get() is { } sagaTenant)
                return sagaTenant;
            var raw = _httpContextAccessor.HttpContext?.Items["TenantId"] as string
                ?? TenantId.Default;
            return new TenantId(raw);
        }
    }
}
