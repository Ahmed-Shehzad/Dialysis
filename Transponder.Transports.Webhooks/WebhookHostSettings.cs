using Transponder.Transports.Abstractions;
using Transponder.Transports.Webhooks.Abstractions;

namespace Transponder.Transports.Webhooks;

/// <summary>
/// Default webhook transport host settings.
/// </summary>
public sealed class WebhookHostSettings : TransportHostSettings, IWebhookHostSettings
{
    public WebhookHostSettings(
        Uri address,
        IReadOnlyList<WebhookSubscription> subscriptions,
        WebhookSignatureOptions? signatureOptions = null,
        TimeSpan? requestTimeout = null,
        IReadOnlyDictionary<string, object?>? settings = null,
        TransportResilienceOptions? resilienceOptions = null)
        : base(address, settings, resilienceOptions)
    {
        Subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        SignatureOptions = signatureOptions ?? new WebhookSignatureOptions();
        RequestTimeout = requestTimeout;
    }

    public IReadOnlyList<WebhookSubscription> Subscriptions { get; }

    public WebhookSignatureOptions SignatureOptions { get; }

    public TimeSpan? RequestTimeout { get; }
}
