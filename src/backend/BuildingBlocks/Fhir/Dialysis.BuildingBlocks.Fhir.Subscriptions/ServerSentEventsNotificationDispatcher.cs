using System.Text;
using Dialysis.BuildingBlocks.Fhir.Serialization;
using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Subscriptions;

/// <summary>
/// Server-Sent-Events channel dispatcher. Writes the Backport IG notification Bundle as a single
/// SSE <c>data:</c> frame to every open <c>text/event-stream</c> response bound to the
/// subscription. Connection-scoped, same drop-if-absent semantics as the WebSocket channel.
/// </summary>
public sealed class ServerSentEventsNotificationDispatcher(
    FhirJsonSerializerProvider serializer,
    FhirSubscriptionConnectionManager connections) : ISubscriptionChannelDispatcher
{
    public SubscriptionChannelType Channel => SubscriptionChannelType.ServerSentEvents;

    public async ValueTask DispatchAsync(
        FhirSubscriptionRegistration subscription,
        IReadOnlyDictionary<string, string> payloadAttributes,
        Resource? payloadResource,
        CancellationToken cancellationToken)
    {
        if (subscription.ChannelType != SubscriptionChannelType.ServerSentEvents)
            return;

        var bundle = SubscriptionNotificationBundleFactory.Build(subscription, payloadResource);
        var json = serializer.Serialize(bundle).ReplaceLineEndings(string.Empty);
        var frame = Encoding.UTF8.GetBytes($"event: subscription-notification\ndata: {json}\n\n");
        await connections.PushAsync(subscription.Id, frame, cancellationToken).ConfigureAwait(false);
    }
}
