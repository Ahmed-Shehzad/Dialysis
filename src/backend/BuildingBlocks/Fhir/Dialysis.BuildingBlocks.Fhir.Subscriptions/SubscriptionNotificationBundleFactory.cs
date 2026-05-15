using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Subscriptions;

/// <summary>
/// Builds the FHIR R4 Subscription Backport IG notification <see cref="Bundle"/> shared by every
/// channel dispatcher (REST-hook, WebSocket, SSE) so subscribers receive an identical wire shape
/// regardless of delivery channel.
/// </summary>
public static class SubscriptionNotificationBundleFactory
{
    private const string BackportNotificationProfile =
        "http://hl7.org/fhir/uv/subscriptions-backport/StructureDefinition/backport-subscription-notification";

    public static Bundle Build(FhirSubscriptionRegistration subscription, Resource? payload)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        var bundle = new Bundle
        {
            Type = Bundle.BundleType.History,
            Timestamp = DateTimeOffset.UtcNow,
            Meta = new Meta { Profile = [BackportNotificationProfile] },
            Identifier = new Identifier(system: "urn:dialysis:fhir:subscription", value: subscription.Id),
        };
        if (payload is not null)
        {
            bundle.Entry.Add(new Bundle.EntryComponent { Resource = payload });
        }
        return bundle;
    }
}
