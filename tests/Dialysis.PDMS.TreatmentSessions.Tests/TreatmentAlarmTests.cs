using Dialysis.PDMS.TreatmentSessions.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.TreatmentSessions.Tests;

public sealed class TreatmentAlarmTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Raise_starts_in_Present_with_matched_timestamps()
    {
        var alarm = TreatmentAlarm.Raise(
            id: Guid.NewGuid(),
            sessionId: Guid.NewGuid(),
            machineId: Guid.NewGuid(),
            alarmCode: 158610L,
            alarmSource: "arterial_pressure_high",
            alarmPhase: "warning",
            observedAtUtc: T0);

        alarm.State.ShouldBe(TreatmentAlarmState.Present);
        alarm.FirstObservedUtc.ShouldBe(T0);
        alarm.LastObservedUtc.ShouldBe(T0);
        alarm.AcknowledgedUtc.ShouldBeNull();
    }

    [Fact]
    public void Refresh_advances_LastObservedUtc_only_forward()
    {
        var alarm = TreatmentAlarm.Raise(Guid.NewGuid(), null, Guid.NewGuid(), 1L, null, null, T0);

        alarm.Refresh(T0.AddSeconds(30));
        alarm.LastObservedUtc.ShouldBe(T0.AddSeconds(30));

        alarm.Refresh(T0.AddSeconds(10));
        alarm.LastObservedUtc.ShouldBe(T0.AddSeconds(30), "Refresh must not move time backwards.");
        alarm.FirstObservedUtc.ShouldBe(T0);
    }

    [Fact]
    public void State_transitions_follow_PA_to_Inactivating_to_Resolved()
    {
        var alarm = TreatmentAlarm.Raise(Guid.NewGuid(), null, Guid.NewGuid(), 1L, null, null, T0);

        alarm.MarkInactivating(T0.AddSeconds(40));
        alarm.State.ShouldBe(TreatmentAlarmState.Inactivating);
        alarm.LastObservedUtc.ShouldBe(T0.AddSeconds(40));

        alarm.MarkResolved(T0.AddSeconds(60));
        alarm.State.ShouldBe(TreatmentAlarmState.Resolved);
        alarm.LastObservedUtc.ShouldBe(T0.AddSeconds(60));
    }

    [Fact]
    public void Refresh_after_Resolved_throws()
    {
        var alarm = TreatmentAlarm.Raise(Guid.NewGuid(), null, Guid.NewGuid(), 1L, null, null, T0);
        alarm.MarkResolved(T0.AddSeconds(10));

        Should.Throw<InvalidOperationException>(() => alarm.Refresh(T0.AddSeconds(20)));
    }

    [Fact]
    public void MarkInactivating_after_Resolved_throws()
    {
        var alarm = TreatmentAlarm.Raise(Guid.NewGuid(), null, Guid.NewGuid(), 1L, null, null, T0);
        alarm.MarkResolved(T0.AddSeconds(10));

        Should.Throw<InvalidOperationException>(() => alarm.MarkInactivating(T0.AddSeconds(20)));
    }

    [Fact]
    public void Acknowledge_records_actor_and_is_idempotent()
    {
        var alarm = TreatmentAlarm.Raise(Guid.NewGuid(), null, Guid.NewGuid(), 1L, null, null, T0);

        alarm.Acknowledge(T0.AddSeconds(5), "nurse-1");
        alarm.AcknowledgedUtc.ShouldBe(T0.AddSeconds(5));
        alarm.AcknowledgedBy.ShouldBe("nurse-1");

        alarm.Acknowledge(T0.AddSeconds(20), "nurse-2");
        alarm.AcknowledgedUtc.ShouldBe(T0.AddSeconds(5), "first ack wins.");
        alarm.AcknowledgedBy.ShouldBe("nurse-1");
    }

    [Fact]
    public void Acknowledge_works_in_Resolved_state()
    {
        var alarm = TreatmentAlarm.Raise(Guid.NewGuid(), null, Guid.NewGuid(), 1L, null, null, T0);
        alarm.MarkResolved(T0.AddSeconds(10));

        alarm.Acknowledge(T0.AddSeconds(15), "nurse-1");

        alarm.AcknowledgedUtc.ShouldBe(T0.AddSeconds(15));
        alarm.State.ShouldBe(TreatmentAlarmState.Resolved);
    }

    [Fact]
    public void Raise_rejects_invalid_inputs()
    {
        Should.Throw<ArgumentException>(() =>
            TreatmentAlarm.Raise(Guid.NewGuid(), null, Guid.Empty, 1L, null, null, T0));
        Should.Throw<ArgumentOutOfRangeException>(() =>
            TreatmentAlarm.Raise(Guid.NewGuid(), null, Guid.NewGuid(), 0L, null, null, T0));
    }
}
