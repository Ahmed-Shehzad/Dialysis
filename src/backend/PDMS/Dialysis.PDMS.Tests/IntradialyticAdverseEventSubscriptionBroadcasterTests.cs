using Dialysis.BuildingBlocks.Fhir.Subscriptions;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.PDMS.Contracts.Integration;
using Dialysis.PDMS.TreatmentSessions.Fhir;
using Hl7.Fhir.Model;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.PDMS.Tests;

public sealed class IntradialyticAdverseEventSubscriptionBroadcasterTests
{
    [Fact]
    public async Task Projects_Adverse_Event_To_Fhir_Adverseevent_And_Dispatches_To_Matching_Subscription_Async()
    {
        var patientId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var registry = new InMemorySubscriptionRegistry();
        await registry.RegisterAsync(
            new FhirSubscriptionRegistration(
                Id: "match",
                TopicUrl: IntradialyticAdverseEventSubscriptionBroadcaster.TopicUrl,
                ChannelType: SubscriptionChannelType.RestHook,
                ChannelEndpoint: "https://example.test/hook",
                ChannelHeader: null,
                FilterParameters: new Dictionary<string, string>(StringComparer.Ordinal) { ["severity"] = "severe" },
                Status: SubscriptionStatus.Active),
            CancellationToken.None);

        var dispatcher = new RecordingDispatcher();
        var broadcaster = new IntradialyticAdverseEventSubscriptionBroadcaster(new SubscriptionBroadcaster(registry, dispatcher));

        var ev = new IntradialyticAdverseEventIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            SessionId: sessionId,
            PatientId: patientId,
            ObservedAtUtc: DateTime.UtcNow,
            EventKindCode: "271594007",
            Severity: "severe",
            Notes: "Symptomatic hypotension mid-session");

        await broadcaster.HandleAsync(new ConsumeContext<IntradialyticAdverseEventIntegrationEvent>(ev, CancellationToken.None, new NoopBus()));

        dispatcher.Dispatched.Count.ShouldBe(1);
        var adverseEvent = dispatcher.Dispatched[0].ShouldBeOfType<AdverseEvent>();
        adverseEvent.Subject.Reference.ShouldBe($"Patient/{patientId}");
        adverseEvent.Event!.Coding[0].Code.ShouldBe("271594007");
        adverseEvent.SuspectEntity[0].Instance.Reference.ShouldBe($"Procedure/{sessionId}");
    }

    [Fact]
    public async Task Does_Not_Dispatch_When_Severity_Filter_Does_Not_Match_Async()
    {
        var registry = new InMemorySubscriptionRegistry();
        await registry.RegisterAsync(
            new FhirSubscriptionRegistration(
                Id: "nomatch",
                TopicUrl: IntradialyticAdverseEventSubscriptionBroadcaster.TopicUrl,
                ChannelType: SubscriptionChannelType.RestHook,
                ChannelEndpoint: "https://example.test/hook",
                ChannelHeader: null,
                FilterParameters: new Dictionary<string, string>(StringComparer.Ordinal) { ["severity"] = "severe" },
                Status: SubscriptionStatus.Active),
            CancellationToken.None);

        var dispatcher = new RecordingDispatcher();
        var broadcaster = new IntradialyticAdverseEventSubscriptionBroadcaster(new SubscriptionBroadcaster(registry, dispatcher));

        var ev = new IntradialyticAdverseEventIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            SessionId: Guid.NewGuid(),
            PatientId: Guid.NewGuid(),
            ObservedAtUtc: DateTime.UtcNow,
            EventKindCode: "271594007",
            Severity: "mild",
            Notes: null);

        await broadcaster.HandleAsync(new ConsumeContext<IntradialyticAdverseEventIntegrationEvent>(ev, CancellationToken.None, new NoopBus()));

        dispatcher.Dispatched.ShouldBeEmpty();
    }

    private sealed class RecordingDispatcher : ISubscriptionNotificationDispatcher
    {
        public List<Resource> Dispatched { get; } = [];

        public ValueTask DispatchAsync(
            FhirSubscriptionRegistration subscription,
            IReadOnlyDictionary<string, string> payloadAttributes,
            Resource? payloadResource,
            CancellationToken cancellationToken)
        {
            if (payloadResource is not null)
                Dispatched.Add(payloadResource);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class NoopBus : ITransponderBus
    {
        public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : class => Task.CompletedTask;
        public Task PublishAsync<TMessage>(TMessage message, TransponderPublishOptions options, CancellationToken cancellationToken = default) where TMessage : class => Task.CompletedTask;
        public Task PublishPreparedAsync(string routingKey, object message, TransponderPublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishLargeAsync<TMessage>(TMessage message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default) where TMessage : class => Task.CompletedTask;
    }
}
