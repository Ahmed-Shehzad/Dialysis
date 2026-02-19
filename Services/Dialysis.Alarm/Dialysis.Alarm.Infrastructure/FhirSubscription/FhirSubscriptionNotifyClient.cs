using System.Net.Http.Json;

using BuildingBlocks.Abstractions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dialysis.Alarm.Infrastructure.FhirSubscription;

public sealed class FhirSubscriptionNotifyClient : IFhirSubscriptionNotifyClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<FhirSubscriptionNotifyClient> _logger;

    public FhirSubscriptionNotifyClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<FhirSubscriptionNotifyClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task NotifyAsync(
        string resourceType,
        string resourceUrl,
        string? tenantId,
        string? authorization,
        CancellationToken cancellationToken = default)
    {
        string? baseUrl = _config["FhirSubscription:NotifyUrl"];
        if (string.IsNullOrEmpty(baseUrl))
            return;

        string notifyUrl = baseUrl.TrimEnd('/') + "/api/fhir/subscription-notify";
        string? apiKey = _config["FhirSubscription:NotifyApiKey"];

        try
        {
            using var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, notifyUrl);
            request.Content = JsonContent.Create(new { resourceType, resourceUrl, tenantId });
            if (!string.IsNullOrEmpty(apiKey))
                _ = request.Headers.TryAddWithoutValidation("X-Subscription-Notify-ApiKey", apiKey);
            if (!string.IsNullOrEmpty(authorization))
                _ = request.Headers.TryAddWithoutValidation("Authorization", authorization);
            if (!string.IsNullOrEmpty(tenantId))
                _ = request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);

            HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("FHIR subscription notify failed: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FHIR subscription notify failed for {ResourceType}", resourceType);
        }
    }
}
