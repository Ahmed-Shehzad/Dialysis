using Dialysis.PDMS.TreatmentSessions.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests;

public sealed class DialysisSessionMachineBindingTests
{
    private static readonly DateTime _scheduledStart = DateTime.UtcNow.AddMinutes(5);

    private static DialysisSession New_Scheduled_Session() => DialysisSession.Schedule(
        id: Guid.NewGuid(),
        patientId: Guid.NewGuid(),
        scheduledStartUtc: _scheduledStart,
        prescription: new SessionPrescription(
            dialyzerModel: "Polyflux 17L",
            prescribedDurationMinutes: 240,
            bloodFlowRateMlPerMin: 350,
            dialysateFlowRateMlPerMin: 500,
            dialysatePotassiumMmolPerL: 2.0m,
            dialysateCalciumMmolPerL: 1.25m,
            dialysateSodiumMmolPerL: 140m,
            targetUfVolumeLiters: 2.5m,
            anticoagulationProtocolCode: "heparin-bolus"),
        access: new VascularAccess(VascularAccessKind.ArteriovenousFistula, "Left forearm", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1))));

    [Fact]
    public void Bind_Machine_Sets_Machine_Id_And_Is_Idempotent_For_Same_Machine()
    {
        var session = New_Scheduled_Session();
        var machineId = Guid.NewGuid();

        session.BindMachine(machineId);
        session.BindMachine(machineId);

        session.MachineId.ShouldBe(machineId);
    }

    [Fact]
    public void Bind_Machine_Rejects_A_Different_Machine()
    {
        var session = New_Scheduled_Session();
        session.BindMachine(Guid.NewGuid());

        Should.Throw<InvalidOperationException>(() => session.BindMachine(Guid.NewGuid()));
    }

    [Fact]
    public void Receive_Observation_Requires_In_Progress_And_Matching_Machine()
    {
        var session = New_Scheduled_Session();
        var machineId = Guid.NewGuid();
        session.BindMachine(machineId);

        Should.Throw<InvalidOperationException>(() => session.ReceiveObservation(machineId, DateTime.UtcNow))
            .Message.ShouldContain("Scheduled");

        session.Start(DateTime.UtcNow);

        session.ReceiveObservation(machineId, DateTime.UtcNow.AddSeconds(1));

        Should.Throw<InvalidOperationException>(() =>
            session.ReceiveObservation(Guid.NewGuid(), DateTime.UtcNow.AddSeconds(2)));
    }

    [Fact]
    public void Receive_Observation_Rejects_Pre_Start_Timestamps()
    {
        var session = New_Scheduled_Session();
        var machineId = Guid.NewGuid();
        session.BindMachine(machineId);
        var start = DateTime.UtcNow;
        session.Start(start);

        Should.Throw<ArgumentException>(() => session.ReceiveObservation(machineId, start.AddMinutes(-1)));
    }

    [Fact]
    public void Pause_And_Resume_Round_Trip()
    {
        var session = New_Scheduled_Session();
        session.BindMachine(Guid.NewGuid());
        var start = DateTime.UtcNow;
        session.Start(start);

        session.Pause(start.AddMinutes(10));
        session.Status.ShouldBe(DialysisSessionStatus.Paused);
        session.PausedAtUtc.ShouldBe(start.AddMinutes(10));

        session.Resume(start.AddMinutes(15));
        session.Status.ShouldBe(DialysisSessionStatus.InProgress);
        session.PausedAtUtc.ShouldBeNull();
        session.AccumulatedPausedDuration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Pause_Rejects_Non_In_Progress()
    {
        var session = New_Scheduled_Session();
        Should.Throw<InvalidOperationException>(() => session.Pause(DateTime.UtcNow));
    }

    [Fact]
    public void Usage_Time_Excludes_Paused_Spans()
    {
        var session = New_Scheduled_Session();
        session.BindMachine(Guid.NewGuid());
        var start = DateTime.UtcNow;
        session.Start(start);

        // Run 60, pause 20, run 60 → 120 min on, 20 min paused.
        session.Pause(start.AddMinutes(60));
        session.Resume(start.AddMinutes(80));
        var completedAt = start.AddMinutes(140);
        session.Complete(completedAt, achievedUfVolumeLiters: 2.5m);

        session.UsageMinutesAsOf(completedAt).ShouldBe(120);
    }

    [Fact]
    public void Usage_Time_Freezes_While_Paused()
    {
        var session = New_Scheduled_Session();
        session.BindMachine(Guid.NewGuid());
        var start = DateTime.UtcNow;
        session.Start(start);
        session.Pause(start.AddMinutes(45));

        // Reference instant is well past the pause, but usage freezes at the pause start.
        session.UsageMinutesAsOf(start.AddMinutes(90)).ShouldBe(45);
    }

    [Fact]
    public void Abort_While_Paused_Excludes_The_Open_Pause()
    {
        var session = New_Scheduled_Session();
        session.BindMachine(Guid.NewGuid());
        var start = DateTime.UtcNow;
        session.Start(start);
        session.Pause(start.AddMinutes(30));

        var abortedAt = start.AddMinutes(50);
        session.Abort(abortedAt, "MACHINE");

        session.AccumulatedPausedDuration.ShouldBe(TimeSpan.FromMinutes(20));
        session.UsageMinutesAsOf(abortedAt).ShouldBe(30);
    }

    [Fact]
    public void Record_Alarm_Requires_Bound_Machine_And_Active_Session()
    {
        var session = New_Scheduled_Session();
        Should.Throw<InvalidOperationException>(() => session.RecordAlarm(Guid.NewGuid(), DateTime.UtcNow));

        var machineId = Guid.NewGuid();
        session.BindMachine(machineId);
        var start = DateTime.UtcNow;
        session.Start(start);

        session.RecordAlarm(machineId, start.AddSeconds(10));

        Should.Throw<InvalidOperationException>(() =>
            session.RecordAlarm(Guid.NewGuid(), start.AddSeconds(20)));
    }
}
