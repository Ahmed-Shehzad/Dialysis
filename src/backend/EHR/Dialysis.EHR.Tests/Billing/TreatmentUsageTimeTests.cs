using Dialysis.Module.Contracts.Billing;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Billing;

/// <summary>
/// Unit tests for <see cref="TreatmentUsageTime"/> — the shared machine-usage-time formatter
/// used by the live chairside timer and every billing / invoice / reporting PDF, so the same
/// duration reads identically everywhere.
/// </summary>
public sealed class TreatmentUsageTimeTests
{
    [Theory]
    [InlineData(0, "0 min")]
    [InlineData(45, "45 min")]
    [InlineData(60, "1 h")]
    [InlineData(120, "2 h")]
    [InlineData(222, "3 h 42 min")]
    [InlineData(-30, "0 min")]
    public void Format_Renders_Compact_Usage_Time(int minutes, string expected) =>
        TreatmentUsageTime.Format(minutes).ShouldBe(expected);
}
