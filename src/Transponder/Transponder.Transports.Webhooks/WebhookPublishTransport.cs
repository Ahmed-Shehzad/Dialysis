using System.Net.Http.Headers;

using Transponder.Transports.Abstractions;
using Transponder.Transports.Webhooks.Abstractions;

namespace Transponder.Transports.Webhooks;

/// <summary>
/// Webhook transport publisher. Delivery can be made durable via outbox and retries.
/// </summary>
internal sealed class WebhookPublishTransport : IPublishTransport
{
    private const string DefaultContentType = "application/octet-stream";
    private readonly HttpClient _httpClient;
    private readonly IWebhookHostSettings _settings;

    public WebhookPublishTransport(HttpClient httpClient, IWebhookHostSettings settings)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public Task PublishAsync(ITransportMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var subscriptions = _settings.Subscriptions;
        if (subscriptions.Count == 0) return Task.CompletedTask;

        var messageType = message.MessageType ?? "unknown";
        var payload = message.Body.Span.ToArray();
        var contentType = string.IsNullOrWhiteSpace(message.ContentType)
            ? DefaultContentType
            : message.ContentType!;

        List<Task> tasks = [];

        foreach (var subscription in subscriptions)
        {
            if (!subscription.Enabled) continue;
            if (!WebhookSubscriptionMatcher.Matches(subscription, messageType)) continue;

            tasks.Add(SendAsync(subscription, message, messageType, contentType, payload, cancellationToken));
        }

        return tasks.Count == 0 ? Task.CompletedTask : Task.WhenAll(tasks);
    }

    private async Task SendAsync(
        WebhookSubscription subscription,
        ITransportMessage message,
        string messageType,
        string contentType,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, subscription.Endpoint);
        var content = new ByteArrayContent(payload);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        request.Content = content;

        AddMetadataHeaders(request, subscription, message, messageType, contentType);
        AddSignatureHeaders(request, subscription, payload);

        using var response = await _httpClient.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
    }

    private void AddMetadataHeaders(
        HttpRequestMessage request,
        WebhookSubscription subscription,
        ITransportMessage message,
        string messageType,
        string contentType)
    {
        if (message.MessageId.HasValue)
            _ = request.Headers.TryAddWithoutValidation(TransponderWebhookHeaders.EventId, message.MessageId.ToString());

        if (!string.IsNullOrWhiteSpace(messageType))
            _ = request.Headers.TryAddWithoutValidation(TransponderWebhookHeaders.EventType, messageType);

        if (message.SentTime.HasValue)
            _ = request.Headers.TryAddWithoutValidation(TransponderWebhookHeaders.SentTime, message.SentTime.Value.ToString("O"));

        if (!string.IsNullOrWhiteSpace(contentType))
            _ = request.Headers.TryAddWithoutValidation(TransponderWebhookHeaders.ContentType, contentType);

        if (message.CorrelationId.HasValue)
            _ = request.Headers.TryAddWithoutValidation(TransponderWebhookHeaders.CorrelationId, message.CorrelationId.ToString());

        if (message.ConversationId.HasValue)
            _ = request.Headers.TryAddWithoutValidation(TransponderWebhookHeaders.ConversationId, message.ConversationId.ToString());

        if (!string.IsNullOrWhiteSpace(subscription.Name))
            _ = request.Headers.TryAddWithoutValidation(TransponderWebhookHeaders.Subscription, subscription.Name);
    }

    private void AddSignatureHeaders(
        HttpRequestMessage request,
        WebhookSubscription subscription,
        byte[] payload)
    {
        if (string.IsNullOrWhiteSpace(subscription.Secret)) return;

        var options = _settings.SignatureOptions;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = WebhookSignature.Compute(subscription.Secret, timestamp, payload, options);

        _ = request.Headers.TryAddWithoutValidation(options.TimestampHeaderName, timestamp);
        _ = request.Headers.TryAddWithoutValidation(options.SignatureHeaderName, signature);
    }
}
