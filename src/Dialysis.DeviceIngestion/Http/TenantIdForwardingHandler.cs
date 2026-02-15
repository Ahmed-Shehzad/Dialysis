using Dialysis.Tenancy;

namespace Dialysis.DeviceIngestion.Http;

/// <summary>Forwards X-Tenant-Id from the current request (or TenantContext) to outgoing FHIR API calls.</summary>
public sealed class TenantIdForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantIdForwardingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        var tenantId = context?.RequestServices.GetService<ITenantContext>()?.TenantId
            ?? context?.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(tenantId))
            request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);
        return await base.SendAsync(request, cancellationToken);
    }
}
