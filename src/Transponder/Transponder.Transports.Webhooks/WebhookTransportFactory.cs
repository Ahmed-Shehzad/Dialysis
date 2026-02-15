using Transponder.Transports.Abstractions;
using Transponder.Transports.Webhooks.Abstractions;

namespace Transponder.Transports.Webhooks;

/// <summary>
/// Factory for webhook transport hosts.
/// </summary>
public sealed class WebhookTransportFactory : ITransportFactory
{
    private static readonly IReadOnlyCollection<string> Schemes =
        ["webhook", "webhooks"];

    public string Name => "Webhooks";

    public IReadOnlyCollection<string> SupportedSchemes => Schemes;

    public ITransportHost CreateHost(ITransportHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings is not IWebhookHostSettings webhookSettings
            ? throw new ArgumentException(
                $"Expected {nameof(IWebhookHostSettings)} but received {settings.GetType().Name}.",
                nameof(settings))
            : new WebhookTransportHost(webhookSettings);
    }
}
