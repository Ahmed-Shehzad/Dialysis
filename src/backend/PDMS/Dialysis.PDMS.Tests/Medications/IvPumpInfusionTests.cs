using Dialysis.PDMS.Medications.Contracts;
using Dialysis.PDMS.Medications.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.Medications;

public sealed class IvPumpInfusionTests
{
    [Fact]
    public void Construction_Records_Start_Event_And_Defaults_Actual_To_Programmed_Rate()
    {
        var infusion = Build_Running_Infusion(rate: 100m, volume: 250m);

        infusion.Status.ShouldBe(IvPumpStatus.Running);
        infusion.ActualRateMlPerHour.ShouldBe(100m);
        infusion.IntegrationEvents.OfType<IvPumpInfusionStartedIntegrationEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Recording_Reading_Updates_Actual_Rate_And_Infused_Volume()
    {
        var infusion = Build_Running_Infusion();
        infusion.RecordReading(actualRateMlPerHour: 99.5m, infusedVolumeMl: 50m);

        infusion.ActualRateMlPerHour.ShouldBe(99.5m);
        infusion.InfusedVolumeMl.ShouldBe(50m);
    }

    [Fact]
    public void Pause_Then_Resume_Transitions_Cleanly()
    {
        var infusion = Build_Running_Infusion();
        infusion.Pause();
        infusion.Status.ShouldBe(IvPumpStatus.Paused);
        infusion.Resume();
        infusion.Status.ShouldBe(IvPumpStatus.Running);
    }

    [Fact]
    public void Resume_From_Non_Paused_State_Throws()
    {
        var infusion = Build_Running_Infusion();
        Should.Throw<InvalidOperationException>(() => infusion.Resume());
    }

    [Fact]
    public void MarkAlarm_Transitions_To_Alarm_And_Raises_Event()
    {
        var infusion = Build_Running_Infusion();
        infusion.MarkAlarm("OCCLUSION_DISTAL", "Distal occlusion detected.", IvPumpAlarmSeverity.Critical, DateTime.UtcNow);

        infusion.Status.ShouldBe(IvPumpStatus.Alarm);
        var alarm = infusion.IntegrationEvents.OfType<IvPumpAlarmRaisedIntegrationEvent>().ShouldHaveSingleItem();
        alarm.AlarmCode.ShouldBe("OCCLUSION_DISTAL");
        alarm.Severity.ShouldBe(IvPumpAlarmSeverity.Critical);
    }

    [Fact]
    public void Complete_Records_Final_Volume_And_Raises_Event()
    {
        var infusion = Build_Running_Infusion(volume: 250m);
        var ended = DateTime.UtcNow;
        infusion.Complete(finalInfusedVolumeMl: 250m, endedAtUtc: ended);

        infusion.Status.ShouldBe(IvPumpStatus.Completed);
        infusion.InfusedVolumeMl.ShouldBe(250m);
        infusion.EndedAtUtc.ShouldBe(ended);
        infusion.IntegrationEvents.OfType<IvPumpInfusionCompletedIntegrationEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Record_Reading_On_Completed_Infusion_Throws()
    {
        var infusion = Build_Running_Infusion();
        infusion.Complete(250m, DateTime.UtcNow);

        Should.Throw<InvalidOperationException>(() => infusion.RecordReading(50m, 100m));
    }

    private static IvPumpInfusion Build_Running_Infusion(decimal rate = 100m, decimal volume = 250m) =>
        new(
            id: Guid.CreateVersion7(),
            sessionId: Guid.CreateVersion7(),
            chairId: Guid.CreateVersion7(),
            pumpDeviceId: "PUMP-CH4-7",
            vendorCode: "bd-alaris",
            medication: MedicationCoding.RxNorm("1234", "Heparin"),
            programmedRateMlPerHour: rate,
            programmedVolumeMl: volume,
            startedAtUtc: DateTime.UtcNow);
}
