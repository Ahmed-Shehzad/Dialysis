using System.Net.Http.Json;

namespace FhirCore.Subscriptions;

public interface IWebhookNotifier
{
    Task NotifyAsync(string endpoint, object payload, CancellationToken cancellationToken = default);
}

public sealed class WebhookNotifier : IWebhookNotifier
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WebhookNotifier(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task NotifyAsync(string endpoint, object payload, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync(endpoint, payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
