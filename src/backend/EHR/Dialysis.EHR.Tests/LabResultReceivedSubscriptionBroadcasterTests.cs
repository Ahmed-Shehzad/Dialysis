using Dialysis.BuildingBlocks.Fhir.Subscriptions;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.EHR.PatientChart.Fhir;
using Hl7.Fhir.Model;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests;

public sealed class LabResultReceivedSubscriptionBroadcasterTests
{
    [Fact]
    public async Task Projects_Lab_Result_To_Observation_And_Dispatches_To_Matching_Subscription_Async()
    {
        var patientId = Guid.NewGuid();
        var registry = new InMemorySubscriptionRegistry();
        await registry.RegisterAsync(
            new FhirSubscriptionRegistration(
                Id: "match",
                TopicUrl: LabResultReceivedSubscriptionBroadcaster.TopicUrl,
                ChannelType: SubscriptionChannelType.RestHook,
                ChannelEndpoint: "https://example.test/hook",
                ChannelHeader: null,
                FilterParameters: new Dictionary<string, string>(StringComparer.Ordinal) { ["patient"] = patientId.ToString() },
                Status: SubscriptionStatus.Active),
            CancellationToken.None);

        var dispatcher = new RecordingDispatcher();
        var broadcaster = new LabResultReceivedSubscriptionBroadcaster(new SubscriptionBroadcaster(registry, dispatcher));

        var ev = new LabResultReceivedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            LabResultId: Guid.NewGuid(),
            LabOrderId: Guid.NewGuid(),
            PatientId: patientId,
            LoincCode: "2345-7",
            ValueText: "210",
            UnitCode: "mg/dL",
            ReferenceRangeText: "70-110",
            AbnormalFlag: "H",
            ObservedAtUtc: DateTime.UtcNow);

        await broadcaster.HandleAsync(new ConsumeContext<LabResultReceivedIntegrationEvent>(ev, CancellationToken.None, new NoopBus()));

        dispatcher.Dispatched.Count.ShouldBe(1);
        var observation = dispatcher.Dispatched[0].ShouldBeOfType<Observation>();
        observation.Code.Coding[0].Code.ShouldBe("2345-7");
        observation.Subject.Reference.ShouldBe($"Patient/{patientId}");
        observation.Value.ShouldBeOfType<Quantity>().Value.ShouldBe(210);
    }

    [Fact]
    public async Task Does_Not_Dispatch_When_Patient_Filter_Does_Not_Match_Async()
    {
        var registry = new InMemorySubscriptionRegistry();
        await registry.RegisterAsync(
            new FhirSubscriptionRegistration(
                Id: "nomatch",
                TopicUrl: LabResultReceivedSubscriptionBroadcaster.TopicUrl,
                ChannelType: SubscriptionChannelType.RestHook,
                ChannelEndpoint: "https://example.test/hook",
                ChannelHeader: null,
                FilterParameters: new Dictionary<string, string>(StringComparer.Ordinal) { ["patient"] = Guid.NewGuid().ToString() },
                Status: SubscriptionStatus.Active),
            CancellationToken.None);

        var dispatcher = new RecordingDispatcher();
        var broadcaster = new LabResultReceivedSubscriptionBroadcaster(new SubscriptionBroadcaster(registry, dispatcher));

        var ev = new LabResultReceivedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            LabResultId: Guid.NewGuid(),
            LabOrderId: Guid.NewGuid(),
            PatientId: Guid.NewGuid(),
            LoincCode: "2345-7",
            ValueText: "95",
            UnitCode: "mg/dL",
            ReferenceRangeText: null,
            AbnormalFlag: "N",
            ObservedAtUtc: DateTime.UtcNow);

        await broadcaster.HandleAsync(new ConsumeContext<LabResultReceivedIntegrationEvent>(ev, CancellationToken.None, new NoopBus()));

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
