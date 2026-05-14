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
        session.Start(DateTime.UtcNow);

        session.Pause();
        session.Status.ShouldBe(DialysisSessionStatus.Paused);

        session.Resume();
        session.Status.ShouldBe(DialysisSessionStatus.InProgress);
    }

    [Fact]
    public void Pause_Rejects_Non_In_Progress()
    {
        var session = New_Scheduled_Session();
        Should.Throw<InvalidOperationException>(() => session.Pause());
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
