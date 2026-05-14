using Dialysis.PDMS.Core;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.TreatmentSessions.Tests;

public sealed class PdmsCoreClockTests
{
    [Fact]
    public void PdmsClock_returns_utc_now_within_one_second()
    {
        IPdmsClock clock = new PdmsClock();

        var before = DateTime.UtcNow;
        var now = clock.UtcNow;
        var after = DateTime.UtcNow;

        now.Kind.ShouldBe(DateTimeKind.Utc);
        now.ShouldBeGreaterThanOrEqualTo(before.AddSeconds(-1));
        now.ShouldBeLessThanOrEqualTo(after.AddSeconds(1));
    }
}
