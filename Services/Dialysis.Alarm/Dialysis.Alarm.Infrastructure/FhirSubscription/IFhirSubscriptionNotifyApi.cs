using Refit;

namespace Dialysis.Alarm.Infrastructure.FhirSubscription;

/// <summary>
/// Refit client for FHIR subscription notify (POST api/fhir/subscription-notify).
/// </summary>
public interface IFhirSubscriptionNotifyApi
{
    [Post("/api/fhir/subscription-notify")]
    Task<HttpResponseMessage> NotifyAsync(
        [Body] SubscriptionNotifyRequest body,
        [Header("X-Subscription-Notify-ApiKey")] string? apiKey,
        [Header("Authorization")] string? authorization,
        [Header("X-Tenant-Id")] string? tenantId,
        CancellationToken cancellationToken = default);
}

public sealed record SubscriptionNotifyRequest(string ResourceType, string ResourceUrl, string? TenantId);
