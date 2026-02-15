using Transponder.Transports.Abstractions;

namespace Transponder.Transports.Webhooks.Abstractions;

/// <summary>
/// Provides webhook-specific settings for creating a transport host.
/// </summary>
public interface IWebhookHostSettings : ITransportHostSettings
{
    /// <summary>
    /// Gets the webhook subscriptions.
    /// </summary>
    IReadOnlyList<WebhookSubscription> Subscriptions { get; }

    /// <summary>
    /// Gets the signature options for webhook delivery.
    /// </summary>
    WebhookSignatureOptions SignatureOptions { get; }

    /// <summary>
    /// Gets the optional request timeout for webhook delivery.
    /// </summary>
    TimeSpan? RequestTimeout { get; }
}
