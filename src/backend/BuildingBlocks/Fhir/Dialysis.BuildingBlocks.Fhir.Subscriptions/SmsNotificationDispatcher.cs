using Hl7.Fhir.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Fhir.Subscriptions;

/// <summary>
/// SMS channel dispatcher. Delegates to a host-supplied <see cref="ISmsNotifier"/>; inert when
/// none is registered. SMS cannot carry a Bundle, so a concise alert text is sent — the resource
/// type + id and the topic, enough for the subscriber to fetch detail out-of-band.
/// </summary>
public sealed class SmsNotificationDispatcher : ISubscriptionChannelDispatcher
{
    private int _missingNotifierLogged;
    private readonly IServiceProvider _services;
    private readonly ILogger<SmsNotificationDispatcher> _logger;
    /// <summary>
    /// SMS channel dispatcher. Delegates to a host-supplied <see cref="ISmsNotifier"/>; inert when
    /// none is registered. SMS cannot carry a Bundle, so a concise alert text is sent — the resource
    /// type + id and the topic, enough for the subscriber to fetch detail out-of-band.
    /// </summary>
    public SmsNotificationDispatcher(IServiceProvider services,
        ILogger<SmsNotificationDispatcher> logger)
    {
        _services = services;
        _logger = logger;
    }

    public SubscriptionChannelType Channel => SubscriptionChannelType.Sms;

    public async ValueTask DispatchAsync(
        FhirSubscriptionRegistration subscription,
        IReadOnlyDictionary<string, string> payloadAttributes,
        Resource? payloadResource,
        CancellationToken cancellationToken)
    {
        if (subscription.ChannelType != SubscriptionChannelType.Sms)
            return;

        var notifier = _services.GetService<ISmsNotifier>();
        if (notifier is null)
        {
            if (Interlocked.Exchange(ref _missingNotifierLogged, 1) == 0)
                _logger.LogWarning("SMS subscription channel is inert: no ISmsNotifier is registered.");
            return;
        }

        var resourceRef = payloadResource is null
            ? "(no payload)"
            : $"{payloadResource.TypeName}/{payloadResource.Id}";
        var message = $"FHIR subscription update: {resourceRef} on topic {subscription.TopicUrl}";

        await notifier.SendAsync(
            new SubscriptionSmsNotification(
                ToNumber: subscription.ChannelEndpoint,
                Message: message,
                Subscription: subscription),
            cancellationToken).ConfigureAwait(false);
    }
}
