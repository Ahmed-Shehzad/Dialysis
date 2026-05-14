using Dialysis.PDMS.Core;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests;

public sealed class PdmsCoreClockTests
{
    [Fact]
    public void Pdms_Clock_Returns_Utc_Now_Within_One_Second()
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
