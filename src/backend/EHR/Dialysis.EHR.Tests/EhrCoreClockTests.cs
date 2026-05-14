using Dialysis.EHR.Core;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests;

[Collection(nameof(EhrFixtureCollection))]
public sealed class EhrCoreClockTests(EhrApiWebApplicationFactory factory)
{
    [Fact]
    public void Iehr_Clock_Resolves_And_Returns_Utc_Now_Within_One_Second()
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
