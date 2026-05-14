using Dialysis.EHR.Core;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests;

[Collection(nameof(EhrFixtureCollection))]
public sealed class EhrCoreClockTests(EhrApiWebApplicationFactory factory)
{
    [Fact]
    public void IEhrClock_resolves_and_returns_utc_now_within_one_second()
    {
        using var scope = factory.Services.CreateScope();
        var clock = scope.ServiceProvider.GetRequiredService<IEhrClock>();

        var before = DateTime.UtcNow;
        var now = clock.UtcNow;
        var after = DateTime.UtcNow;

        now.Kind.ShouldBe(DateTimeKind.Utc);
        now.ShouldBeGreaterThanOrEqualTo(before.AddSeconds(-1));
        now.ShouldBeLessThanOrEqualTo(after.AddSeconds(1));
    }
}
