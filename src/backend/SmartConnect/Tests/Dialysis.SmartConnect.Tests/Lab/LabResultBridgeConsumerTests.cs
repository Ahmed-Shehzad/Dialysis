using System.Text;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.Lab.Contracts;
using Dialysis.Lab.Contracts.IntegrationEvents;
using Dialysis.SmartConnect.Api.Lab;
using Dialysis.SmartConnect.Contracts.Integration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Lab;

/// <summary>
/// Coverage for the host-side bridge that turns a routed inbound ORU^R01 into the Lab context's
/// typed LabResultReceivedIntegrationEvent — including the routing-hint gate that lets non-lab
/// payloads pass straight through.
/// </summary>
public sealed class LabResultBridgeConsumerTests
{
    private static readonly Guid _patientId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private const string Oru =
        "MSH|^~\\&|LIS||DIALYSIS||20260605101500||ORU^R01^ORU_R01|MSG-7|P|2.5\r" +
        "PID|||22222222-2222-2222-2222-222222222222^^^^MR\r" +
        "ORC|RE|LAB-ABC123|FILL-7788\r" +
        "OBR|1|LAB-ABC123|FILL-7788|2160-0^Creatinine^LN|||20260605101000\r" +
        "OBX|1|NM|2160-0^Creatinine^LN||1.4|mg/dL|0.6-1.3|H|||F\r";

    [Fact]
    public async Task Bridges_Oru_To_Typed_Lab_Result_Event_Async()
    {
        var bus = new RecordingBus();
        var consumer = new LabResultBridgeConsumer(NullLogger<LabResultBridgeConsumer>.Instance);

        await consumer.HandleAsync(Context(RoutedPayload(LabResultBridgeConsumer.LabResultRoutingHint, Oru), bus));

        var published = Assert.Single(bus.Published);
        var ev = Assert.IsType<LabResultReceivedIntegrationEvent>(published);
        Assert.Equal("LAB-ABC123", ev.PlacerOrderNumber);
        Assert.Equal("FILL-7788", ev.FillerOrderNumber);
        Assert.Equal(_patientId, ev.PatientId);
        Assert.Equal(LabOrderStatus.Resulted, ev.Status);
        var obs = Assert.Single(ev.Observations);
        Assert.Equal("2160-0", obs.LoincCode);
        Assert.Equal("1.4", obs.Value);
        Assert.Equal(LabResultInterpretation.High, obs.Interpretation);
    }

    [Fact]
    public async Task Ignores_Payload_With_Non_Lab_Routing_Hint_Async()
    {
        var bus = new RecordingBus();
        var consumer = new LabResultBridgeConsumer(NullLogger<LabResultBridgeConsumer>.Instance);

        await consumer.HandleAsync(Context(RoutedPayload("some.other.hint", Oru), bus));

        Assert.Empty(bus.Published);
    }

    [Fact]
    public async Task Ignores_Lab_Hint_Payload_That_Is_Not_An_Oru_Async()
    {
        var bus = new RecordingBus();
        var consumer = new LabResultBridgeConsumer(NullLogger<LabResultBridgeConsumer>.Instance);

        await consumer.HandleAsync(Context(
            RoutedPayload(LabResultBridgeConsumer.LabResultRoutingHint, "not an hl7 message"), bus));

        Assert.Empty(bus.Published);
    }

    private static SmartConnectRoutedPayloadIntegrationEvent RoutedPayload(string hint, string payload) =>
        new(
            EventId: Guid.NewGuid(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            FlowId: Guid.NewGuid(),
            IntegrationMessageId: Guid.NewGuid(),
            OutboundRouteOrdinal: 0,
            RoutingHint: hint,
            PayloadFormat: "Utf8Text",
            Payload: Encoding.UTF8.GetBytes(payload),
            Headers: new Dictionary<string, string>());

    private static ConsumeContext<SmartConnectRoutedPayloadIntegrationEvent> Context(
        SmartConnectRoutedPayloadIntegrationEvent ev, ITransponderBus bus) =>
        new(ev, CancellationToken.None, bus);

    private sealed class RecordingBus : ITransponderBus
    {
        public List<object> Published { get; } = [];

        public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : class
        {
            Published.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishAsync<TMessage>(TMessage message, TransponderPublishOptions options, CancellationToken cancellationToken = default) where TMessage : class
        {
            Published.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishPreparedAsync(string routingKey, object message, TransponderPublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishLargeAsync<TMessage>(TMessage message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default) where TMessage : class => Task.CompletedTask;
    }
}
