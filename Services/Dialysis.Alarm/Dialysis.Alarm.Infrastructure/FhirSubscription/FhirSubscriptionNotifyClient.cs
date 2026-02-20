using BuildingBlocks.Abstractions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dialysis.Alarm.Infrastructure.FhirSubscription;

public sealed class FhirSubscriptionNotifyClient : IFhirSubscriptionNotifyClient
{
    private readonly IFhirSubscriptionNotifyApi _api;
    private readonly IConfiguration _config;
    private readonly ILogger<FhirSubscriptionNotifyClient> _logger;

    public FhirSubscriptionNotifyClient(
        IFhirSubscriptionNotifyApi api,
        IConfiguration config,
        ILogger<FhirSubscriptionNotifyClient> logger)
    {
        _api = api;
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

        string? apiKey = _config["FhirSubscription:NotifyApiKey"];

        try
        {
            HttpResponseMessage response = await _api.NotifyAsync(
                new SubscriptionNotifyRequest(resourceType, resourceUrl, tenantId),
                apiKey,
                authorization,
                tenantId,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("FHIR subscription notify failed: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FHIR subscription notify failed for {ResourceType}", resourceType);
        }
    }
}
