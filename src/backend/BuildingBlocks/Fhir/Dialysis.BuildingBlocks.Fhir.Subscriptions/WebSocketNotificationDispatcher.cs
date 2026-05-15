using System.Text;
using Dialysis.BuildingBlocks.Fhir.Serialization;
using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Subscriptions;

/// <summary>
/// WebSocket channel dispatcher. Pushes the Backport IG notification Bundle to every WebSocket
/// connection bound to the subscription via <see cref="FhirSubscriptionConnectionManager"/>.
/// Connection-scoped: if no client is currently bound the notification is dropped (the client
/// receives subsequent events after it reconnects and re-binds), so no failure is recorded.
/// </summary>
public sealed class WebSocketNotificationDispatcher(
    FhirJsonSerializerProvider serializer,
    FhirSubscriptionConnectionManager connections) : ISubscriptionChannelDispatcher
{
    public SubscriptionChannelType Channel => SubscriptionChannelType.WebSocket;

    public async ValueTask DispatchAsync(
        FhirSubscriptionRegistration subscription,
        IReadOnlyDictionary<string, string> payloadAttributes,
        Resource? payloadResource,
        CancellationToken cancellationToken)
    {
        if (subscription.ChannelType != SubscriptionChannelType.WebSocket)
            return;

        var bundle = SubscriptionNotificationBundleFactory.Build(subscription, payloadResource);
        var bytes = Encoding.UTF8.GetBytes(serializer.Serialize(bundle));
        await connections.PushAsync(subscription.Id, bytes, cancellationToken).ConfigureAwait(false);
    }
}
