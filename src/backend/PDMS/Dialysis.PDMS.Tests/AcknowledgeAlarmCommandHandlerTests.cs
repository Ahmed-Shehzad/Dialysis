using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.TreatmentSessions.Domain;
using Dialysis.PDMS.TreatmentSessions.Features.AcknowledgeAlarm;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.PDMS.Tests;

public sealed class AcknowledgeAlarmCommandHandlerTests
{
    private static readonly DateTime _t0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_Stamps_Acknowledger_From_Command_And_Saves_Async()
    {
        var alarm = TreatmentAlarm.Raise(Guid.NewGuid(), null, Guid.NewGuid(), 158610L, "arterial_pressure_high", "warning", _t0);
        var repo = new InMemoryAlarms(alarm);
        var uow = new RecordingUnitOfWork();
        var clock = new FakeTimeProvider(_t0.AddSeconds(5));
        var handler = new AcknowledgeAlarmCommandHandler(repo, uow, clock);

        await handler.HandleAsync(new AcknowledgeAlarmCommand(alarm.Id, "nurse-1"), CancellationToken.None);

        alarm.AcknowledgedUtc.ShouldBe(_t0.AddSeconds(5));
        alarm.AcknowledgedBy.ShouldBe("nurse-1");
        uow.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_Throws_When_Alarm_Missing_Async()
    {
        var handler = new AcknowledgeAlarmCommandHandler(
            new InMemoryAlarms(), new RecordingUnitOfWork(), new FakeTimeProvider(_t0));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new AcknowledgeAlarmCommand(Guid.NewGuid(), "nurse-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Is_Idempotent_For_Already_Acknowledged_Alarms_Async()
    {
        var alarm = TreatmentAlarm.Raise(Guid.NewGuid(), null, Guid.NewGuid(), 1L, null, null, _t0);
        alarm.Acknowledge(_t0.AddSeconds(2), "nurse-1");
        var repo = new InMemoryAlarms(alarm);
        var handler = new AcknowledgeAlarmCommandHandler(
            repo, new RecordingUnitOfWork(), new FakeTimeProvider(_t0.AddSeconds(10)));

        await handler.HandleAsync(new AcknowledgeAlarmCommand(alarm.Id, "nurse-2"), CancellationToken.None);

        alarm.AcknowledgedUtc.ShouldBe(_t0.AddSeconds(2), "first acknowledger wins.");
        alarm.AcknowledgedBy.ShouldBe("nurse-1");
    }

    private sealed class InMemoryAlarms(params TreatmentAlarm[] seed) : ITreatmentAlarmRepository
    {
        private readonly List<TreatmentAlarm> _alarms = [.. seed];

        public void Add(TreatmentAlarm alarm) => _alarms.Add(alarm);

        public Task<TreatmentAlarm?> GetAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_alarms.FirstOrDefault(a => a.Id == id));

        public Task<TreatmentAlarm?> FindLiveAsync(Guid machineId, long alarmCode, CancellationToken cancellationToken = default)
            => Task.FromResult(_alarms.FirstOrDefault(a =>
                a.MachineId == machineId &&
                a.AlarmCode == alarmCode &&
                a.State != TreatmentAlarmState.Resolved));

        public Task<IReadOnlyList<TreatmentAlarm>> ListActiveAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TreatmentAlarm>>([.. _alarms.Where(a => a.State != TreatmentAlarmState.Resolved)]);
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeTimeProvider(DateTime utcNow) : TimeProvider
    {
        private readonly DateTime _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => new(_utcNow, TimeSpan.Zero);
    }
}
