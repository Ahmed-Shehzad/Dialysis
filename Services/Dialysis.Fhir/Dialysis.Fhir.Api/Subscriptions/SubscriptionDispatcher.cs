using Dialysis.Fhir.Abstractions;

using Hl7.Fhir.Model;

namespace Dialysis.Fhir.Api.Subscriptions;

/// <summary>
/// Evaluates FHIR subscriptions and dispatches rest-hook notifications.
/// </summary>
public sealed class SubscriptionDispatcher
{
    private readonly ISubscriptionStore _store;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SubscriptionDispatcher> _logger;

    public SubscriptionDispatcher(
        ISubscriptionStore store,
        HttpClient httpClient,
        ILogger<SubscriptionDispatcher> logger)
    {
        _store = store;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Notify matching subscriptions about a new/updated FHIR resource.
    /// Fetches the resource from resourceUrl, evaluates criteria, and POSTs to rest-hook endpoints.
    /// </summary>
    public async System.Threading.Tasks.Task DispatchAsync(
        string resourceType,
        string resourceUrl,
        string? tenantId,
        string? authorizationHeader,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(resourceUrl))
            return;

        string criteriaResourceType = resourceType.Trim();
        IReadOnlyList<Subscription> matching = GetMatchingSubscriptions(criteriaResourceType);
        if (matching.Count == 0)
            return;

        Bundle? sourceBundle = await FetchResourceBundleAsync(resourceUrl, tenantId, authorizationHeader, cancellationToken);
        if (sourceBundle?.Entry is null)
            return;

        string payload = BuildNotificationPayload(sourceBundle, resourceType);
        await PostToSubscribersAsync(matching, payload, tenantId, cancellationToken);
    }

    private IReadOnlyList<Subscription> GetMatchingSubscriptions(string resourceType)
    {
        IReadOnlyList<Subscription> subscriptions = _store.GetActiveRestHookSubscriptions();
        return subscriptions.Where(s => MatchesCriteria(s.Criteria, resourceType)).ToList();
    }

    private async Task<Bundle?> FetchResourceBundleAsync(
        string resourceUrl,
        string? tenantId,
        string? authorizationHeader,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, resourceUrl);
        if (!string.IsNullOrEmpty(authorizationHeader))
            _ = request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
        if (!string.IsNullOrEmpty(tenantId))
            _ = request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);
        _ = request.Headers.TryAddWithoutValidation("Accept", "application/fhir+json");

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Subscription dispatcher failed to fetch resource from {ResourceUrl}: {StatusCode}",
                resourceUrl, response.StatusCode);
            return null;
        }

        string bundleJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return Hl7ToFhir.FhirJsonHelper.FromJson<Bundle>(bundleJson);
    }

    private static string BuildNotificationPayload(Bundle sourceBundle, string resourceType)
    {
        var filtered = sourceBundle.Entry
            .Where(e => e.Resource is not null && string.Equals(e.Resource.TypeName, resourceType, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var notificationBundle = new Bundle
        {
            Type = Bundle.BundleType.Collection,
            Entry = filtered.Count > 0 ? filtered : sourceBundle.Entry
        };

        return Hl7ToFhir.FhirJsonHelper.ToJson(notificationBundle);
    }

    private async System.Threading.Tasks.Task PostToSubscribersAsync(
        IReadOnlyList<Subscription> matching,
        string payload,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        foreach (Subscription sub in matching)
        {
            string? endpoint = sub.Channel?.Endpoint;
            if (string.IsNullOrEmpty(endpoint))
                continue;

            try
            {
                using var postRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
                postRequest.Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/fhir+json");
                if (!string.IsNullOrEmpty(tenantId))
                    _ = postRequest.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);

                using HttpResponseMessage postResponse = await _httpClient.SendAsync(postRequest, cancellationToken);
                if (!postResponse.IsSuccessStatusCode)
                    _logger.LogWarning("Subscription {SubId} rest-hook to {Endpoint} returned {StatusCode}",
                        sub.Id, endpoint, postResponse.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscription {SubId} rest-hook to {Endpoint} failed", sub.Id, endpoint);
            }
        }
    }

    private static bool MatchesCriteria(string? criteria, string resourceType)
    {
        if (string.IsNullOrEmpty(criteria))
            return false;

        string typePart = criteria.Split('?')[0].Trim();
        return string.Equals(typePart, resourceType, StringComparison.OrdinalIgnoreCase);
    }
}
