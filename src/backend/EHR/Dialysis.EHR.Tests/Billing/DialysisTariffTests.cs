using Dialysis.Module.Contracts.Billing;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Billing;

/// <summary>
/// Unit tests for the shared <see cref="DialysisTariff"/> — the single calculation behind both
/// the PDMS live cost estimate and the EHR invoice, so they must agree.
/// </summary>
public sealed class DialysisTariffTests
{
    [Fact]
    public void Compute_Itemises_Setup_Time_And_Ultrafiltration()
    {
        var result = DialysisTariff.Compute("HD", durationMinutes: 240, ufLiters: 2.5m);

        result.CurrencyCode.ShouldBe("USD");
        result.Lines.Count.ShouldBe(3);
        // 120 setup + (1.50 × 240) + (15.00 × 2.5) = 120 + 360 + 37.50
        result.Total.ShouldBe(517.50m);
        result.Lines[1].Amount.ShouldBe(360.00m);
        result.Lines[2].Amount.ShouldBe(37.50m);
    }

    [Fact]
    public void Compute_Clamps_Negative_Quantities_To_Zero()
    {
        var result = DialysisTariff.Compute("HD", durationMinutes: -30, ufLiters: -1m);

        // Only the flat setup fee survives.
        result.Total.ShouldBe(120.00m);
    }

    [Fact]
    public void Compute_Honours_Overridden_Rates()
    {
        var options = new DialysisTariffOptions
        {
            SetupFee = 100m,
            PerMinuteRate = 2m,
            PerLiterUfRate = 10m,
            CurrencyCode = "EUR",
        };

        var result = DialysisTariff.Compute("PD", durationMinutes: 60, ufLiters: 1.5m, options);

        result.CurrencyCode.ShouldBe("EUR");
        // 100 + (2 × 60) + (10 × 1.5) = 100 + 120 + 15
        result.Total.ShouldBe(235.00m);
    }
}
