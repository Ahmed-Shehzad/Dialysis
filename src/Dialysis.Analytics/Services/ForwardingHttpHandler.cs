using Dialysis.Tenancy;
using Microsoft.AspNetCore.Http;

namespace Dialysis.Analytics.Services;

/// <summary>Forwards Authorization and X-Tenant-Id from the current HTTP context to outgoing requests.</summary>
public sealed class ForwardingHttpHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantContext _tenantContext;

    public ForwardingHttpHandler(IHttpContextAccessor httpContextAccessor, ITenantContext tenantContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantContext = tenantContext;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null)
        {
            var auth = context.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(auth))
                request.Headers.TryAddWithoutValidation("Authorization", auth);

            var tenantId = _tenantContext.TenantId ?? "default";
            request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
