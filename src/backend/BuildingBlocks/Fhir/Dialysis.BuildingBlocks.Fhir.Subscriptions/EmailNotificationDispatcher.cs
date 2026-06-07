using Dialysis.BuildingBlocks.Fhir.Serialization;
using Hl7.Fhir.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Fhir.Subscriptions;

/// <summary>
/// Email channel dispatcher. Delegates to a host-supplied <see cref="IEmailNotifier"/>; when none
/// is registered the channel is inert (logged once per process), per the plan's "interfaces only,
/// modules supply their own" rule.
/// </summary>
public sealed class EmailNotificationDispatcher : ISubscriptionChannelDispatcher
{
    private int _missingNotifierLogged;
    private readonly IServiceProvider _services;
    private readonly ILogger<EmailNotificationDispatcher> _logger;
    /// <summary>
    /// Email channel dispatcher. Delegates to a host-supplied <see cref="IEmailNotifier"/>; when none
    /// is registered the channel is inert (logged once per process), per the plan's "interfaces only,
    /// modules supply their own" rule.
    /// </summary>
    public EmailNotificationDispatcher(IServiceProvider services,
        ILogger<EmailNotificationDispatcher> logger)
    {
        _services = services;
        _logger = logger;
    }

    public SubscriptionChannelType Channel => SubscriptionChannelType.Email;

    public async ValueTask DispatchAsync(
        FhirSubscriptionRegistration subscription,
        IReadOnlyDictionary<string, string> payloadAttributes,
        Resource? payloadResource,
        CancellationToken cancellationToken)
    {
        if (subscription.ChannelType != SubscriptionChannelType.Email)
            return;

        var notifier = _services.GetService<IEmailNotifier>();
        if (notifier is null)
        {
            if (Interlocked.Exchange(ref _missingNotifierLogged, 1) == 0)
                _logger.LogWarning("Email subscription channel is inert: no IEmailNotifier is registered.");
            return;
        }

        var bundle = SubscriptionNotificationBundleFactory.Build(subscription, payloadResource);
        var json = FhirJsonSerializerProvider.Serialize(bundle);
        await notifier.SendAsync(
            new SubscriptionEmailNotification(
                ToAddress: subscription.ChannelEndpoint,
                Subject: $"FHIR subscription notification ({subscription.TopicUrl})",
                FhirBundleJson: json,
                Subscription: subscription),
            cancellationToken).ConfigureAwait(false);
    }
}
