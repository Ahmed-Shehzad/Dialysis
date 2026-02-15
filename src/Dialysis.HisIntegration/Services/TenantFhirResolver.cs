using Dialysis.HisIntegration.Features.AdtSync;
using Dialysis.Tenancy;
using Microsoft.Extensions.Options;

namespace Dialysis.HisIntegration.Services;

public interface ITenantFhirResolver
{
    string? GetBaseUrl(string? tenantId);
}

public sealed class TenantFhirResolver : ITenantFhirResolver
{
    private readonly FhirAdtWriterOptions _options;
    private readonly ITenantContext _tenantContext;

    public TenantFhirResolver(IOptions<FhirAdtWriterOptions> options, ITenantContext tenantContext)
    {
        _options = options.Value;
        _tenantContext = tenantContext;
    }

    public string? GetBaseUrl(string? tenantId)
    {
        var tid = tenantId ?? _tenantContext.TenantId ?? "default";
        if (_options.TenantBaseUrls is { Count: > 0 } && _options.TenantBaseUrls.TryGetValue(tid, out var url) && !string.IsNullOrEmpty(url))
            return url.TrimEnd('/');
        return string.IsNullOrEmpty(_options.BaseUrl) ? null : _options.BaseUrl.TrimEnd('/');
    }
}
