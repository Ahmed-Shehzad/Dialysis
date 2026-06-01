using Dialysis.PDMS.Medications.Contracts;
using Dialysis.PDMS.OnCall.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.OnCall;

/// <summary>
/// Lifecycle smoke tests for the escalation policy. The dispatcher reads the per-severity
/// windows; getting these wrong delays alarm escalation, so the assertions are explicit.
/// </summary>
public sealed class EscalationPolicyTests
{
    [Fact]
    public void Default_Critical_Walks_Primary_To_Backup_To_Supervisor()
    {
        var policy = EscalationPolicy.CreateDefault(Guid.NewGuid());

        policy.DelayBeforeNextLink(IvPumpAlarmSeverity.Critical, 0).ShouldBe(TimeSpan.FromSeconds(60));
        policy.DelayBeforeNextLink(IvPumpAlarmSeverity.Critical, 1).ShouldBe(TimeSpan.FromSeconds(120));
        policy.DelayBeforeNextLink(IvPumpAlarmSeverity.Critical, 2).ShouldBeNull();
    }

    [Fact]
    public void Default_Warning_Has_Longer_Windows_Than_Critical()
    {
        var policy = EscalationPolicy.CreateDefault(Guid.NewGuid());

        policy.DelayBeforeNextLink(IvPumpAlarmSeverity.Warning, 0).ShouldBe(TimeSpan.FromMinutes(5));
        policy.DelayBeforeNextLink(IvPumpAlarmSeverity.Warning, 1).ShouldBe(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void Informational_Only_Pages_Primary()
    {
        var policy = EscalationPolicy.CreateDefault(Guid.NewGuid());

        policy.DelayBeforeNextLink(IvPumpAlarmSeverity.Informational, 0).ShouldBe(TimeSpan.FromMinutes(15));
        policy.DelayBeforeNextLink(IvPumpAlarmSeverity.Informational, 1).ShouldBeNull();
    }

    [Fact]
    public void Negative_Or_Zero_Window_Rejected()
    {
        var act = () => new EscalationPolicy(
            Guid.NewGuid(), "broken",
            TimeSpan.Zero, TimeSpan.FromSeconds(1),
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2),
            TimeSpan.FromMinutes(5),
            quietHoursSuppressNonCritical: true);

        Should.Throw<ArgumentOutOfRangeException>(act);
    }
}
