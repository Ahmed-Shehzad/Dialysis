using Dialysis.PDMS.TreatmentSessions.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests;

public sealed class TreatmentAlarmTests
{
    private static readonly DateTime _T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Raise_Starts_In_Present_With_Matched_Timestamps()
    {
        var alarm = TreatmentAlarm.Raise(
            id: Guid.NewGuid(),
            sessionId: Guid.NewGuid(),
            machineId: Guid.NewGuid(),
            alarmCode: 158610L,
            alarmSource: "arterial_pressure_high",
            alarmPhase: "warning",
            observedAtUtc: _T0);

        alarm.State.ShouldBe(TreatmentAlarmState.Present);
        alarm.FirstObservedUtc.ShouldBe(_T0);
        alarm.LastObservedUtc.ShouldBe(_T0);
        alarm.AcknowledgedUtc.ShouldBeNull();
    }

    [Fact]
    public void Refresh_Advances_Last_Observed_Utc_Only_Forward()
    {
        var alarm = TreatmentAlarm.Raise(Guid.NewGuid(), null, Guid.NewGuid(), 1L, null, null, _T0);

        alarm.Refresh(_T0.AddSeconds(30));
        alarm.LastObservedUtc.ShouldBe(_T0.AddSeconds(30));

        alarm.Refresh(_T0.AddSeconds(10));
        alarm.LastObservedUtc.ShouldBe(_T0.AddSeconds(30), "Refresh must not move time backwards.");
        alarm.FirstObservedUtc.ShouldBe(_T0);
    }

    [Fact]
    public void State_Transitions_Follow_Pa_To_Inactivating_To_Resolved()
    {
        var alarm = TreatmentAlarm.Raise(Guid.NewGuid(), null, Guid.NewGuid(), 1L, null, null, _T0);

        alarm.MarkInactivating(_T0.AddSeconds(40));
        alarm.State.ShouldBe(TreatmentAlarmState.Inactivating);
        alarm.LastObservedUtc.ShouldBe(_T0.AddSeconds(40));

        alarm.MarkResolved(_T0.AddSeconds(60));
        alarm.State.ShouldBe(TreatmentAlarmState.Resolved);
        alarm.LastObservedUtc.ShouldBe(_T0.AddSeconds(60));
    }

    [Fact]
    public void Refresh_After_Resolved_Throws()
    {
        var alarm = TreatmentAlarm.Raise(Guid.NewGuid(), null, Guid.NewGuid(), 1L, null, null, _T0);
        alarm.MarkResolved(_T0.AddSeconds(10));

        Should.Throw<InvalidOperationException>(() => alarm.Refresh(_T0.AddSeconds(20)));
    }

    [Fact]
    public void Mark_Inactivating_After_Resolved_Throws()
    {
        var alarm = TreatmentAlarm.Raise(Guid.NewGuid(), null, Guid.NewGuid(), 1L, null, null, _T0);
        alarm.MarkResolved(_T0.AddSeconds(10));

        Should.Throw<InvalidOperationException>(() => alarm.MarkInactivating(_T0.AddSeconds(20)));
    }

    [Fact]
    public void Acknowledge_Records_Actor_And_Is_Idempotent()
    {
        var alarm = TreatmentAlarm.Raise(Guid.NewGuid(), null, Guid.NewGuid(), 1L, null, null, _T0);

        alarm.Acknowledge(_T0.AddSeconds(5), "nurse-1");
        alarm.AcknowledgedUtc.ShouldBe(_T0.AddSeconds(5));
        alarm.AcknowledgedBy.ShouldBe("nurse-1");

        alarm.Acknowledge(_T0.AddSeconds(20), "nurse-2");
        alarm.AcknowledgedUtc.ShouldBe(_T0.AddSeconds(5), "first ack wins.");
        alarm.AcknowledgedBy.ShouldBe("nurse-1");
    }

    [Fact]
    public void Acknowledge_Works_In_Resolved_State()
    {
        var alarm = TreatmentAlarm.Raise(Guid.NewGuid(), null, Guid.NewGuid(), 1L, null, null, _T0);
        alarm.MarkResolved(_T0.AddSeconds(10));

        alarm.Acknowledge(_T0.AddSeconds(15), "nurse-1");

        alarm.AcknowledgedUtc.ShouldBe(_T0.AddSeconds(15));
        alarm.State.ShouldBe(TreatmentAlarmState.Resolved);
    }

    [Fact]
    public void Raise_Rejects_Invalid_Inputs()
    {
        Should.Throw<ArgumentException>(() =>
            TreatmentAlarm.Raise(Guid.NewGuid(), null, Guid.Empty, 1L, null, null, _T0));
        Should.Throw<ArgumentOutOfRangeException>(() =>
            TreatmentAlarm.Raise(Guid.NewGuid(), null, Guid.NewGuid(), 0L, null, null, _T0));
    }
}
