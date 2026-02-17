using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.AspNetCore.Http;

namespace Dialysis.Gateway.Infrastructure;

/// <summary>
/// Resolves tenant from HTTP context. Implements ITenantContext for DeviceIngestion.
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
            var raw = _httpContextAccessor.HttpContext?.Items["TenantId"] as string
                ?? TenantId.Default;
            return new TenantId(raw);
        }
    }
}
