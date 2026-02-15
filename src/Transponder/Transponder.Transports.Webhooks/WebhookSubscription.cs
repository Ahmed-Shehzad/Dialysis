namespace Transponder.Transports.Webhooks;

/// <summary>
/// Represents a webhook subscription.
/// </summary>
public sealed class WebhookSubscription
{
    public WebhookSubscription(
        Uri endpoint,
        string? name = null,
        string? secret = null,
        IReadOnlyList<string>? eventTypes = null,
        bool enabled = true)
    {
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        Name = name;
        Secret = secret;
        EventTypes = eventTypes ?? Array.Empty<string>();
        Enabled = enabled;
    }

    public Uri Endpoint { get; }

    public string? Name { get; }

    public string? Secret { get; }

    public IReadOnlyList<string> EventTypes { get; }

    public bool Enabled { get; }
}
