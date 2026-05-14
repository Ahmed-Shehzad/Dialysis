using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Dialysis.BuildingBlocks.Fhir.Serialization;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.Subscriptions;

/// <summary>
/// REST-hook channel dispatcher. POSTs a subscription notification Bundle (per the Backport IG)
/// to the subscriber's URL with an HMAC signature header derived from the subscription secret.
/// </summary>
public sealed class RestHookNotificationDispatcher(
    IHttpClientFactory httpClientFactory,
    FhirJsonSerializerProvider serializer,
    ISubscriptionRegistry registry,
    ILogger<RestHookNotificationDispatcher> logger) : ISubscriptionNotificationDispatcher
{
    private const int MaxConsecutiveFailures = 5;

    public async ValueTask DispatchAsync(
        FhirSubscriptionRegistration subscription,
        IReadOnlyDictionary<string, string> payloadAttributes,
        Resource? payloadResource,
        CancellationToken cancellationToken)
    {
        if (subscription.ChannelType != SubscriptionChannelType.RestHook)
            return;

        var bundle = BuildNotificationBundle(subscription, payloadResource);
        var json = serializer.Serialize(bundle);
        var body = Encoding.UTF8.GetBytes(json);

        using var client = httpClientFactory.CreateClient(nameof(RestHookNotificationDispatcher));
        using var request = new HttpRequestMessage(HttpMethod.Post, subscription.ChannelEndpoint)
        {
            Content = new ByteArrayContent(body)
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/fhir+json") { CharSet = "utf-8" } },
            },
        };
        if (!string.IsNullOrEmpty(subscription.ChannelHeader))
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(subscription.ChannelHeader));
            var signature = Convert.ToHexString(hmac.ComputeHash(body));
            request.Headers.TryAddWithoutValidation("X-Fhir-Subscription-Signature", signature);
        }

        try
        {
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                await RecordFailureAsync(subscription, cancellationToken).ConfigureAwait(false);
                return;
            }
            await RecordSuccessAsync(subscription, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "REST-hook delivery failed for subscription {SubscriptionId}", subscription.Id);
            await RecordFailureAsync(subscription, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task RecordSuccessAsync(FhirSubscriptionRegistration subscription, CancellationToken cancellationToken)
    {
        if (subscription.ConsecutiveFailures == 0) return Task.CompletedTask;
        return registry.UpdateStatusAsync(subscription.Id, SubscriptionStatus.Active, consecutiveFailures: 0, cancellationToken).AsTask();
    }

    private Task RecordFailureAsync(FhirSubscriptionRegistration subscription, CancellationToken cancellationToken)
    {
        var nextFailureCount = subscription.ConsecutiveFailures + 1;
        var nextStatus = nextFailureCount >= MaxConsecutiveFailures ? SubscriptionStatus.Error : subscription.Status;
        return registry.UpdateStatusAsync(subscription.Id, nextStatus, nextFailureCount, cancellationToken).AsTask();
    }

    private static Bundle BuildNotificationBundle(FhirSubscriptionRegistration subscription, Resource? payload)
    {
        var bundle = new Bundle
        {
            Type = Bundle.BundleType.History,
            Timestamp = DateTimeOffset.UtcNow,
        };
        bundle.Meta = new Meta
        {
            Profile = ["http://hl7.org/fhir/uv/subscriptions-backport/StructureDefinition/backport-subscription-notification"],
        };
        if (payload is not null)
        {
            bundle.Entry.Add(new Bundle.EntryComponent { Resource = payload });
        }
        bundle.Identifier = new Identifier(system: "urn:dialysis:fhir:subscription", value: subscription.Id);
        return bundle;
    }
}
