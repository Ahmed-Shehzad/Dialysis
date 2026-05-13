using Dialysis.PDMS.TreatmentSessions.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.TreatmentSessions.Tests;

public sealed class DialysisMachineTests
{
    [Fact]
    public void Register_normalizes_optional_metadata()
    {
        var machine = DialysisMachine.Register(Guid.NewGuid(), "  SN-001  ", "  FMC  ", null);

        machine.SerialNumber.ShouldBe("SN-001");
        machine.VendorCode.ShouldBe("FMC");
        machine.ModelCode.ShouldBeNull();
        machine.LastSeenUtc.ShouldBeNull();
        machine.CurrentSessionId.ShouldBeNull();
    }

    [Fact]
    public void Touch_advances_LastSeenUtc_only_forward()
    {
        var machine = DialysisMachine.Register(Guid.NewGuid(), "SN-001", null, null);
        var t = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        machine.Touch(t);
        machine.LastSeenUtc.ShouldBe(t);

        machine.Touch(t.AddMinutes(-10));
        machine.LastSeenUtc.ShouldBe(t, "Touch must not move time backwards.");

        machine.Touch(t.AddMinutes(5));
        machine.LastSeenUtc.ShouldBe(t.AddMinutes(5));
    }

    [Fact]
    public void BindToSession_and_ReleaseFromSession_round_trip()
    {
        var machine = DialysisMachine.Register(Guid.NewGuid(), "SN-001", null, null);
        var sessionId = Guid.NewGuid();

        machine.BindToSession(sessionId);
        machine.CurrentSessionId.ShouldBe(sessionId);

        machine.ReleaseFromSession();
        machine.CurrentSessionId.ShouldBeNull();
    }

    [Fact]
    public void BindToSession_rejects_empty_id()
    {
        var machine = DialysisMachine.Register(Guid.NewGuid(), "SN-001", null, null);
        Should.Throw<ArgumentException>(() => machine.BindToSession(Guid.Empty));
    }

    [Fact]
    public void Register_rejects_blank_serial()
    {
        Should.Throw<ArgumentException>(() => DialysisMachine.Register(Guid.NewGuid(), "  ", null, null));
    }
}
