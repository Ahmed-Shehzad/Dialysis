using Dialysis.PDMS.OnCall.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.OnCall;

/// <summary>
/// Rotation chain + shift coverage. The consumer reads <c>LinkForAttempt</c> on each
/// escalation step, so the 0/1/2 → primary/backup/supervisor mapping must be stable.
/// </summary>
public sealed class OnCallRotationTests
{
    private static OnCallChainLink Link(string name) =>
        new(name, name, [new NotificationChannelTarget(NotificationChannel.Sms, "+10000000000")]);

    [Fact]
    public void Link_For_Attempt_Maps_0_To_Primary_1_To_Backup_2_To_Supervisor()
    {
        var rotation = new OnCallRotation(
            Guid.NewGuid(), Guid.NewGuid(), OnCallShift.Morning,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 12, 31),
            primary: Link("alice"), backup: Link("bob"), supervisor: Link("carol"));

        rotation.LinkForAttempt(0).ShouldNotBeNull().ClinicianSub.ShouldBe("alice");
        rotation.LinkForAttempt(1).ShouldNotBeNull().ClinicianSub.ShouldBe("bob");
        rotation.LinkForAttempt(2).ShouldNotBeNull().ClinicianSub.ShouldBe("carol");
        rotation.LinkForAttempt(3).ShouldBeNull();
    }

    [Fact]
    public void Morning_Shift_Covers_10am_But_Not_11pm()
    {
        var rotation = new OnCallRotation(
            Guid.NewGuid(), Guid.NewGuid(), OnCallShift.Morning,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30),
            Link("p"), Link("b"), Link("s"));

        rotation.CoversInstant(new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc)).ShouldBeTrue();
        rotation.CoversInstant(new DateTime(2026, 6, 15, 23, 0, 0, DateTimeKind.Utc)).ShouldBeFalse();
    }

    [Fact]
    public void Night_Shift_Wraps_Midnight()
    {
        var rotation = new OnCallRotation(
            Guid.NewGuid(), Guid.NewGuid(), OnCallShift.Night,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30),
            Link("p"), Link("b"), Link("s"));

        rotation.CoversInstant(new DateTime(2026, 6, 15, 23, 30, 0, DateTimeKind.Utc)).ShouldBeTrue();
        rotation.CoversInstant(new DateTime(2026, 6, 15, 3, 0, 0, DateTimeKind.Utc)).ShouldBeTrue();
        rotation.CoversInstant(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc)).ShouldBeFalse();
    }

    [Fact]
    public void Rotation_Outside_Effective_Window_Is_Not_Covered()
    {
        var rotation = new OnCallRotation(
            Guid.NewGuid(), Guid.NewGuid(), OnCallShift.Morning,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30),
            Link("p"), Link("b"), Link("s"));

        rotation.CoversInstant(new DateTime(2026, 5, 31, 10, 0, 0, DateTimeKind.Utc)).ShouldBeFalse();
        rotation.CoversInstant(new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc)).ShouldBeFalse();
    }
}
