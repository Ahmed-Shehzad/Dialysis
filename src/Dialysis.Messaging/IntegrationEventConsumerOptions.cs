namespace Dialysis.Messaging;

/// <summary>
/// Configuration for an integration event consumer endpoint.
/// </summary>
public class IntegrationEventConsumerOptions<TMessage>
{
    /// <summary>
    /// The input address (topic + subscription for Azure Service Bus).
    /// Example: sb://dialysis/observation-created/subscriptions/prediction-subscription
    /// </summary>
    public Uri? InputAddress { get; set; }
}
