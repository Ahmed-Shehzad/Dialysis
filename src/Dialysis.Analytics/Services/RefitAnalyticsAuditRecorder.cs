using System.Security.Claims;
using Dialysis.ApiClients;
using Dialysis.Analytics.Configuration;
using Microsoft.Extensions.Options;

namespace Dialysis.Analytics.Services;

/// <summary>Records audit events via Refit IAuditConsentApi.</summary>
public sealed class RefitAnalyticsAuditRecorder : IAnalyticsAuditRecorder
{
    private readonly IAuditConsentApi _api;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AnalyticsOptions _options;

    public RefitAnalyticsAuditRecorder(IAuditConsentApi api, IHttpContextAccessor httpContextAccessor, IOptions<AnalyticsOptions> options)
    {
        _api = api;
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
    }

    public async Task RecordAsync(string resourceType, string resourceId, string action, string? agentId = null, string? outcome = "0", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AuditConsentBaseUrl)) return;

        var agent = agentId ?? _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var payload = new RecordAuditRequest(resourceType, resourceId, action, agent, outcome);

        try
        {
            await _api.RecordAuditAsync(payload, cancellationToken);
        }
        catch
        {
            // Fire-and-forget: do not fail the request if audit fails
        }
    }
}
