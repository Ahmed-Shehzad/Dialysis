using Dialysis.BuildingBlocks.Fhir.Subscriptions;
using Hl7.Fhir.Model;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.Tests.Subscriptions;

public sealed class SubscriptionBroadcasterTests
{
    private const string Topic = "https://dialysis.local/fhir/SubscriptionTopic/lab-result";

    [Fact]
    public async Task Broadcast_Dispatches_Only_To_Subscriptions_Whose_Filters_Match_Async()
    {
        var registry = new InMemorySubscriptionRegistry();
        await registry.RegisterAsync(NewSub("match", new() { ["patient"] = "p1" }), CancellationToken.None);
        await registry.RegisterAsync(NewSub("nomatch", new() { ["patient"] = "p2" }), CancellationToken.None);

        var dispatcher = new RecordingDispatcher();
        var broadcaster = new SubscriptionBroadcaster(registry, dispatcher);

        await broadcaster.BroadcastAsync(
            Topic,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["patient"] = "p1" },
            new Observation { Id = "o1" },
            CancellationToken.None);

        dispatcher.Dispatched.Count.ShouldBe(1);
        dispatcher.Dispatched[0].Id.ShouldBe("match");
    }

    [Fact]
    public async Task Broadcast_Is_Noop_When_No_Active_Subscription_Matches_Async()
    {
        var registry = new InMemorySubscriptionRegistry();
        var dispatcher = new RecordingDispatcher();
        var broadcaster = new SubscriptionBroadcaster(registry, dispatcher);

        await broadcaster.BroadcastAsync(
            Topic,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["patient"] = "p1" },
            payloadResource: null,
            CancellationToken.None);

        dispatcher.Dispatched.ShouldBeEmpty();
    }

    private static FhirSubscriptionRegistration NewSub(string id, Dictionary<string, string> filters) => new(
        Id: id,
        TopicUrl: Topic,
        ChannelType: SubscriptionChannelType.RestHook,
        ChannelEndpoint: "https://example.test/hook",
        ChannelHeader: null,
        FilterParameters: filters,
        Status: SubscriptionStatus.Active);

    private sealed class RecordingDispatcher : ISubscriptionNotificationDispatcher
    {
        public List<FhirSubscriptionRegistration> Dispatched { get; } = [];

        public ValueTask DispatchAsync(
            FhirSubscriptionRegistration subscription,
            IReadOnlyDictionary<string, string> payloadAttributes,
            Resource? payloadResource,
            CancellationToken cancellationToken)
        {
            Dispatched.Add(subscription);
            return ValueTask.CompletedTask;
        }
    }
}
