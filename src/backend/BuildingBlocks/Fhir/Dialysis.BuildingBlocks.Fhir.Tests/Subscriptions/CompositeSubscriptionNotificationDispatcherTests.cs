using Dialysis.BuildingBlocks.Fhir.Subscriptions;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.Tests.Subscriptions;

public sealed class CompositeSubscriptionNotificationDispatcherTests
{
    [Fact]
    public async Task Routes_To_The_Channel_Dispatcher_Matching_The_Subscription_Channel_Async()
    {
        var restHook = new RecordingChannel(SubscriptionChannelType.RestHook);
        var websocket = new RecordingChannel(SubscriptionChannelType.WebSocket);
        var composite = new CompositeSubscriptionNotificationDispatcher(
            [restHook, websocket],
            NullLogger<CompositeSubscriptionNotificationDispatcher>.Instance);

        await composite.DispatchAsync(
            NewSub(SubscriptionChannelType.WebSocket),
            new Dictionary<string, string>(StringComparer.Ordinal),
            new Observation { Id = "o1" },
            CancellationToken.None);

        websocket.Count.ShouldBe(1);
        restHook.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Is_Noop_When_No_Channel_Dispatcher_Is_Registered_For_The_Channel_Async()
    {
        var restHook = new RecordingChannel(SubscriptionChannelType.RestHook);
        var composite = new CompositeSubscriptionNotificationDispatcher(
            [restHook],
            NullLogger<CompositeSubscriptionNotificationDispatcher>.Instance);

        await composite.DispatchAsync(
            NewSub(SubscriptionChannelType.ServerSentEvents),
            new Dictionary<string, string>(StringComparer.Ordinal),
            payloadResource: null,
            CancellationToken.None);

        restHook.Count.ShouldBe(0);
    }

    private static FhirSubscriptionRegistration NewSub(SubscriptionChannelType channel) => new(
        Id: "s1",
        TopicUrl: "https://dialysis.local/fhir/SubscriptionTopic/lab-result",
        ChannelType: channel,
        ChannelEndpoint: "x",
        ChannelHeader: null,
        FilterParameters: new Dictionary<string, string>(StringComparer.Ordinal),
        Status: SubscriptionStatus.Active);

    private sealed class RecordingChannel : ISubscriptionChannelDispatcher
    {
        private readonly SubscriptionChannelType _channel;
        public RecordingChannel(SubscriptionChannelType channel) => _channel = channel;
        public SubscriptionChannelType Channel => _channel;

        public int Count { get; private set; }

        public ValueTask DispatchAsync(
            FhirSubscriptionRegistration subscription,
            IReadOnlyDictionary<string, string> payloadAttributes,
            Resource? payloadResource,
            CancellationToken cancellationToken)
        {
            Count++;
            return ValueTask.CompletedTask;
        }
    }
}
