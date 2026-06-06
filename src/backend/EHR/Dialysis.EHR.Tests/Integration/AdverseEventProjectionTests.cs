using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Integration.Consumers;
using Dialysis.EHR.Integration.Features.Surveillance;
using Dialysis.EHR.Integration.Ports;
using Dialysis.EHR.Integration.ReadModels;
using Dialysis.PDMS.Contracts.Integration;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests.Integration;

/// <summary>
/// The cross-patient adverse-event surveillance read model: PDMS intradialytic events are projected into
/// rows (idempotent on the source event id) and rolled up by kind/severity with a spike flag.
/// </summary>
public sealed class AdverseEventProjectionTests
{
    private static readonly DateTime _now = new(2026, 6, 6, 12, 0, 0, DateTimeKind.Utc);

    private static IntradialyticAdverseEventIntegrationEvent Event(Guid eventId, Guid patient, string kind, string severity, DateTime observed) =>
        new(eventId, observed, 1, Guid.NewGuid(), patient, observed, kind, severity, "note");

    [Fact]
    public async Task Projects_An_Adverse_Event_Row_Async()
    {
        var repo = new InMemoryAdverseEvents();
        var patient = Guid.NewGuid();
        await new AdverseEventProjector(repo, new NoopUnitOfWork())
            .HandleAsync(Ctx(Event(Guid.CreateVersion7(), patient, "271594007", "Blocking", _now)));

        var row = repo.Rows.ShouldHaveSingleItem();
        row.PatientId.ShouldBe(patient);
        row.Kind.ShouldBe("271594007");
        row.Severity.ShouldBe("Blocking");
    }

    [Fact]
    public async Task Redelivery_Of_The_Same_Event_Is_Idempotent_Async()
    {
        var repo = new InMemoryAdverseEvents();
        var evt = Event(Guid.CreateVersion7(), Guid.NewGuid(), "271594007", "Blocking", _now);
        var projector = new AdverseEventProjector(repo, new NoopUnitOfWork());

        await projector.HandleAsync(Ctx(evt));
        await projector.HandleAsync(Ctx(evt));

        repo.Rows.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Surveillance_Counts_Buckets_And_Flags_A_Spike_Async()
    {
        var repo = new InMemoryAdverseEvents();
        // Current window (last 7 days): 4 hypotension + 1 cramp. Baseline (prior 7 days): 1 hypotension.
        for (var i = 0; i < 4; i++)
            await repo.RecordAsync(Row("271594007", "Blocking", _now.AddDays(-1)));
        await repo.RecordAsync(Row("45352006", "Warning", _now.AddDays(-2)));
        await repo.RecordAsync(Row("271594007", "Blocking", _now.AddDays(-10)));

        var handler = new GetAdverseEventSurveillanceQueryHandler(repo, new FixedClock(_now));
        var result = await handler.HandleAsync(new GetAdverseEventSurveillanceQuery(WindowDays: 7), CancellationToken.None);

        result.Total.ShouldBe(5); // current window only
        result.Buckets.First(b => b.Kind == "271594007").Count.ShouldBe(4);
        result.Spikes.ShouldHaveSingleItem().Kind.ShouldBe("271594007");
    }

    private static AdverseEventRecord Row(string kind, string severity, DateTime occurred) => new()
    {
        Id = Guid.CreateVersion7(),
        PatientId = Guid.NewGuid(),
        SessionId = Guid.NewGuid(),
        Kind = kind,
        Severity = severity,
        OccurredAtUtc = occurred,
        SourceEventKey = Guid.NewGuid().ToString(),
    };

    private static ConsumeContext<T> Ctx<T>(T message) where T : class =>
        new(message, CancellationToken.None, new NoopBus());

    private sealed class InMemoryAdverseEvents : IAdverseEventRepository
    {
        public List<AdverseEventRecord> Rows { get; } = [];
        public Task RecordAsync(AdverseEventRecord e, CancellationToken cancellationToken = default)
        {
            if (!Rows.Any(r => r.SourceEventKey == e.SourceEventKey))
                Rows.Add(e);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<AdverseEventRecord>> ListSinceAsync(DateTime sinceUtc, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AdverseEventRecord>>([.. Rows.Where(r => r.OccurredAtUtc >= sinceUtc).OrderByDescending(r => r.OccurredAtUtc).Take(take)]);
        public Task<IReadOnlyList<AdverseEventRecord>> ListForPatientAsync(Guid patientId, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AdverseEventRecord>>([.. Rows.Where(r => r.PatientId == patientId).Take(take)]);
    }

    private sealed class NoopUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class FixedClock : TimeProvider
    {
        private readonly DateTime _utcNow;
        public FixedClock(DateTime utcNow) => _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => new(_utcNow, TimeSpan.Zero);
    }

    private sealed class NoopBus : ITransponderBus
    {
        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
        public Task PublishAsync<T>(T message, TransponderPublishOptions options, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
        public Task PublishPreparedAsync(string routingKey, object message, TransponderPublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishLargeAsync<T>(T message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
    }
}
