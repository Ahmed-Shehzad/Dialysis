using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Dialysis.SmartConnect.Alerts.Actions;

/// <summary>
/// Sends an HTTP request when an alert fires. Properties JSON shape:
/// <c>{"url":"https://...","method":"POST","headers":{"X-Token":"..."},"contentType":"application/json","bodyTemplate":"..."}</c>.
/// <see cref="AlertVariables"/> is applied to <c>bodyTemplate</c> and to every header value.
/// </summary>
public sealed class WebhookAlertActionProvider(IHttpClientFactory httpClientFactory) : IAlertActionProvider
{
    public const string KindValue = "webhook";
    public const string HttpClientName = "smartconnect-outbound";

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public string Kind => KindValue;

    public async Task<AlertActionResult> ExecuteAsync(
        AlertEvent evt,
        AlertRule rule,
        AlertActionSlot slot,
        CancellationToken cancellationToken)
    {
        WebhookProperties? props;
        try
        {
            props = string.IsNullOrWhiteSpace(slot.PropertiesJson)
                ? null
                : JsonSerializer.Deserialize<WebhookProperties>(slot.PropertiesJson, JsonOpts);
        }
        catch (JsonException ex)
        {
            return AlertActionResult.Failure($"Invalid webhook action properties JSON: {ex.Message}");
        }
        if (props is null || string.IsNullOrWhiteSpace(props.Url))
        {
            return AlertActionResult.Failure("Webhook action requires 'url' property.");
        }

        var method = string.IsNullOrWhiteSpace(props.Method) ? HttpMethod.Post : new HttpMethod(props.Method);
        var contentType = string.IsNullOrWhiteSpace(props.ContentType) ? "application/json" : props.ContentType;
        var body = AlertVariables.Render(props.BodyTemplate ?? JsonSerializer.Serialize(evt), evt, rule);

        using var request = new HttpRequestMessage(method, props.Url)
        {
            Content = new StringContent(body, Encoding.UTF8, contentType),
        };
        if (props.Headers is not null)
        {
            foreach (var kvp in props.Headers)
            {
                var rendered = AlertVariables.Render(kvp.Value, evt, rule);
                if (!request.Headers.TryAddWithoutValidation(kvp.Key, rendered))
                {
                    request.Content!.Headers.TryAddWithoutValidation(kvp.Key, rendered);
                }
            }
        }

        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return AlertActionResult.Success($"{(int)response.StatusCode} {response.ReasonPhrase}");
            }
            return AlertActionResult.Failure($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            return AlertActionResult.Failure(ex.Message);
        }
    }

    private sealed class WebhookProperties
    {
        public string? Url { get; set; }
        public string? Method { get; set; }
        public string? ContentType { get; set; }
        public string? BodyTemplate { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
    }
}
