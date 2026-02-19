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

        IReadOnlyList<Subscription> subscriptions = _store.GetActiveRestHookSubscriptions();
        List<Subscription> matching = [];
        string criteriaResourceType = resourceType.Trim();

        foreach (Subscription sub in subscriptions)
        {
            if (MatchesCriteria(sub.Criteria, criteriaResourceType))
                matching.Add(sub);
        }

        if (matching.Count == 0)
            return;

        using var request = new HttpRequestMessage(HttpMethod.Get, resourceUrl);
        if (!string.IsNullOrEmpty(authorizationHeader))
            _ = request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
        if (!string.IsNullOrEmpty(tenantId))
            _ = request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);
        request.Headers.TryAddWithoutValidation("Accept", "application/fhir+json");

        HttpResponseMessage? fetchResponse = await _httpClient.SendAsync(request, cancellationToken);
        if (!fetchResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Subscription dispatcher failed to fetch resource from {ResourceUrl}: {StatusCode}",
                resourceUrl, fetchResponse.StatusCode);
            return;
        }

        string bundleJson = await fetchResponse.Content.ReadAsStringAsync(cancellationToken);
        Bundle? sourceBundle = Dialysis.Hl7ToFhir.FhirJsonHelper.FromJson<Bundle>(bundleJson);
        if (sourceBundle?.Entry is null)
            return;

        var notificationBundle = new Bundle
        {
            Type = Bundle.BundleType.Collection,
            Entry = sourceBundle.Entry
                .Where(e => e.Resource is not null && string.Equals(e.Resource.TypeName, resourceType, StringComparison.OrdinalIgnoreCase))
                .ToList()
        };

        if (notificationBundle.Entry.Count == 0)
            notificationBundle.Entry.AddRange(sourceBundle.Entry);

        string payload = Dialysis.Hl7ToFhir.FhirJsonHelper.ToJson(notificationBundle);

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

                HttpResponseMessage postResponse = await _httpClient.SendAsync(postRequest, cancellationToken);
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
