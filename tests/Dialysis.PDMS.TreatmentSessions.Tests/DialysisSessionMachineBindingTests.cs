using Dialysis.PDMS.TreatmentSessions.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.TreatmentSessions.Tests;

public sealed class DialysisSessionMachineBindingTests
{
    private static readonly DateTime ScheduledStart = DateTime.UtcNow.AddMinutes(5);

    private static DialysisSession NewScheduledSession() => DialysisSession.Schedule(
        id: Guid.NewGuid(),
        patientId: Guid.NewGuid(),
        scheduledStartUtc: ScheduledStart,
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
    public void BindMachine_sets_MachineId_and_is_idempotent_for_same_machine()
    {
        var session = NewScheduledSession();
        var machineId = Guid.NewGuid();

        session.BindMachine(machineId);
        session.BindMachine(machineId);

        session.MachineId.ShouldBe(machineId);
    }

    [Fact]
    public void BindMachine_rejects_a_different_machine()
    {
        var session = NewScheduledSession();
        session.BindMachine(Guid.NewGuid());

        Should.Throw<InvalidOperationException>(() => session.BindMachine(Guid.NewGuid()));
    }

    [Fact]
    public void ReceiveObservation_requires_InProgress_and_matching_machine()
    {
        var session = NewScheduledSession();
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
    public void ReceiveObservation_rejects_pre_start_timestamps()
    {
        var session = NewScheduledSession();
        var machineId = Guid.NewGuid();
        session.BindMachine(machineId);
        var start = DateTime.UtcNow;
        session.Start(start);

        Should.Throw<ArgumentException>(() => session.ReceiveObservation(machineId, start.AddMinutes(-1)));
    }

    [Fact]
    public void Pause_and_Resume_round_trip()
    {
        var session = NewScheduledSession();
        session.BindMachine(Guid.NewGuid());
        session.Start(DateTime.UtcNow);

        session.Pause();
        session.Status.ShouldBe(DialysisSessionStatus.Paused);

        session.Resume();
        session.Status.ShouldBe(DialysisSessionStatus.InProgress);
    }

    [Fact]
    public void Pause_rejects_non_InProgress()
    {
        var session = NewScheduledSession();
        Should.Throw<InvalidOperationException>(() => session.Pause());
    }

    [Fact]
    public void RecordAlarm_requires_bound_machine_and_active_session()
    {
        var session = NewScheduledSession();
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
