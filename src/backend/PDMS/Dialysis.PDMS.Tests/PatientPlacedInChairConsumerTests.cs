using Dialysis.BuildingBlocks.Transponder;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;
using Dialysis.PDMS.TreatmentSessions.Features.IngestChairAssignment;
using Dialysis.PDMS.TreatmentSessions.Projections;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.PDMS.Tests;

public sealed class PatientPlacedInChairConsumerTests
{
    private static readonly DateTime _t0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Place_Updates_Projection_With_Latest_Assignment_Per_Chair_Async()
    {
        var projection = new ChairOccupancyProjection();
        var consumer = new PatientPlacedInChairConsumer(projection, NullLogger<PatientPlacedInChairConsumer>.Instance);
        var patientA = Guid.NewGuid();
        var patientB = Guid.NewGuid();

        await consumer.HandleAsync(Context(Placement(patientA, "C-01", _t0)));
        await consumer.HandleAsync(Context(Placement(patientB, "C-02", _t0.AddMinutes(5))));

        var current = projection.List();
        current.Count.ShouldBe(2);
        current.Single(a => a.Chair == "C-01").PatientId.ShouldBe(patientA);
        current.Single(a => a.Chair == "C-02").PatientId.ShouldBe(patientB);
    }

    [Fact]
    public async Task Place_On_Already_Occupied_Chair_Overwrites_Prior_Occupant_Async()
    {
        var projection = new ChairOccupancyProjection();
        var consumer = new PatientPlacedInChairConsumer(projection, NullLogger<PatientPlacedInChairConsumer>.Instance);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        await consumer.HandleAsync(Context(Placement(first, "C-07", _t0)));
        await consumer.HandleAsync(Context(Placement(second, "C-07", _t0.AddHours(4))));

        var current = projection.List();
        current.Count.ShouldBe(1);
        current[0].PatientId.ShouldBe(second);
        current[0].PlacedAtUtc.ShouldBe(_t0.AddHours(4));
    }

    private static PatientPlacedInChairIntegrationEvent Placement(Guid patientId, string chair, DateTime at) =>
        new(
            EventId: Guid.NewGuid(),
            OccurredOn: at,
            SchemaVersion: 1,
            EntryId: Guid.NewGuid(),
            PatientId: patientId,
            Chair: chair,
            PlacedAtUtc: at);

    private static ConsumeContext<PatientPlacedInChairIntegrationEvent> Context(PatientPlacedInChairIntegrationEvent message) =>
        new(message, CancellationToken.None, new NoopBus());

    private sealed class NoopBus : ITransponderBus
    {
        public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : class => Task.CompletedTask;
        public Task PublishAsync<TMessage>(TMessage message, TransponderPublishOptions options, CancellationToken cancellationToken = default) where TMessage : class => Task.CompletedTask;
        public Task PublishPreparedAsync(string routingKey, object message, TransponderPublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishLargeAsync<TMessage>(TMessage message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default) where TMessage : class => Task.CompletedTask;
    }
}
