using Bogus;
using Dialysis.Prediction.Services;
using Shouldly;
using Microsoft.Extensions.Options;
using Xunit;

namespace Dialysis.Tests;

public sealed class RiskScorerTests
{
    private readonly Faker _faker = new();

    [Fact]
    public void BaselineRiskScorer_empty_vitals_returns_zero()
    {
        var scorer = new BaselineRiskScorer();
        scorer.CalculateRisk("p1", []).ShouldBe(0.0);
    }

    [Fact]
    public void BaselineRiskScorer_null_patient_id_handled()
    {
        var scorer = new BaselineRiskScorer();
        var vitals = new List<VitalSnapshot> { new(VitalCodes.SystolicBp, 85, DateTimeOffset.UtcNow) };
        scorer.CalculateRisk("", vitals).ShouldBeGreaterThanOrEqualTo(0.8);
    }

    [Theory]
    [InlineData(85, 0.85)]
    [InlineData(89, 0.85)]
    [InlineData(90, 0.6)]
    [InlineData(99, 0.6)]
    [InlineData(100, 0.2)]
    [InlineData(120, 0.2)]
    public void BaselineRiskScorer_systolic_boundaries(double systolic, double expectedMin)
    {
        var scorer = new BaselineRiskScorer();
        var vitals = new List<VitalSnapshot> { new(VitalCodes.SystolicBp, systolic, DateTimeOffset.UtcNow) };
        var risk = scorer.CalculateRisk("p1", vitals);
        risk.ShouldBeGreaterThanOrEqualTo(expectedMin - 0.05);
        risk.ShouldBeLessThanOrEqualTo(expectedMin + 0.2);
    }

    [Fact]
    public void BaselineRiskScorer_uses_blood_pressure_systolic_alt_code()
    {
        var scorer = new BaselineRiskScorer();
        var vitals = new List<VitalSnapshot> { new(VitalCodes.SystolicBpAlt, 85, DateTimeOffset.UtcNow) };
        scorer.CalculateRisk("p1", vitals).ShouldBeGreaterThanOrEqualTo(0.8);
    }

    [Fact]
    public void BaselineRiskScorer_averages_multiple_readings()
    {
        var scorer = new BaselineRiskScorer();
        var now = DateTimeOffset.UtcNow;
        var vitals = new List<VitalSnapshot>
        {
            new(VitalCodes.SystolicBp, 90, now),
            new(VitalCodes.SystolicBp, 100, now.AddMinutes(-1))
        };
        var risk = scorer.CalculateRisk("p1", vitals);
        risk.ShouldBe(0.6);
    }

    [Fact]
    public void BaselineRiskScorer_ignores_non_systolic_vitals()
    {
        var scorer = new BaselineRiskScorer();
        var vitals = new List<VitalSnapshot>
        {
            new(VitalCodes.HeartRate, 120, DateTimeOffset.UtcNow),
            new(VitalCodes.DiastolicBp, 60, DateTimeOffset.UtcNow)
        };
        scorer.CalculateRisk("p1", vitals).ShouldBe(0.0);
    }

    [Fact]
    public void EnhancedRiskScorer_empty_vitals_returns_zero()
    {
        var scorer = new EnhancedRiskScorer(Options.Create(new RiskScorerOptions()));
        scorer.CalculateRisk("p1", []).ShouldBe(0.0);
    }

    [Fact]
    public void EnhancedRiskScorer_null_options_uses_defaults()
    {
        var scorer = new EnhancedRiskScorer(null);
        var vitals = new List<VitalSnapshot> { new(VitalCodes.SystolicBp, 85, DateTimeOffset.UtcNow) };
        scorer.CalculateRisk("p1", vitals).ShouldBeGreaterThanOrEqualTo(0.8);
    }

    [Theory]
    [InlineData(85, 0.9)]
    [InlineData(89, 0.9)]
    [InlineData(90, 0.65)]
    [InlineData(99, 0.65)]
    [InlineData(100, 0.0)]
    [InlineData(120, 0.0)]
    public void EnhancedRiskScorer_systolic_thresholds(double systolic, double expectedMin)
    {
        var options = Options.Create(new RiskScorerOptions
        {
            SystolicCriticalThreshold = 90,
            SystolicWarningThreshold = 100
        });
        var scorer = new EnhancedRiskScorer(options);
        var vitals = new List<VitalSnapshot> { new(VitalCodes.SystolicBp, systolic, DateTimeOffset.UtcNow) };
        var risk = scorer.CalculateRisk("p1", vitals);
        risk.ShouldBeGreaterThanOrEqualTo(expectedMin - 0.1);
    }

    [Fact]
    public void EnhancedRiskScorer_trend_drop_adds_risk()
    {
        var scorer = new EnhancedRiskScorer(Options.Create(new RiskScorerOptions()));
        var now = DateTimeOffset.UtcNow;
        var vitals = new List<VitalSnapshot>
        {
            new(VitalCodes.SystolicBp, 95, now),
            new(VitalCodes.SystolicBp, 110, now.AddMinutes(-2))
        };
        scorer.CalculateRisk("p1", vitals).ShouldBeGreaterThanOrEqualTo(0.5);
    }

    [Fact]
    public void EnhancedRiskScorer_tachycardia_adds_risk()
    {
        var options = Options.Create(new RiskScorerOptions { TachycardiaThreshold = 100 });
        var scorer = new EnhancedRiskScorer(options);
        var vitals = new List<VitalSnapshot>
        {
            new(VitalCodes.SystolicBp, 105, DateTimeOffset.UtcNow),
            new(VitalCodes.HeartRate, 120, DateTimeOffset.UtcNow)
        };
        scorer.CalculateRisk("p1", vitals).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void EnhancedRiskScorer_spO2_critical_adds_risk()
    {
        var options = Options.Create(new RiskScorerOptions { Spo2CriticalThreshold = 92 });
        var scorer = new EnhancedRiskScorer(options);
        var vitals = new List<VitalSnapshot>
        {
            new(VitalCodes.SpO2, 88, DateTimeOffset.UtcNow)
        };
        scorer.CalculateRisk("p1", vitals).ShouldBeGreaterThanOrEqualTo(0.7);
    }

    [Fact]
    public void EnhancedRiskScorer_returns_clamped_between_0_and_1()
    {
        var scorer = new EnhancedRiskScorer(Options.Create(new RiskScorerOptions()));
        var vitals = Enumerable.Range(0, 20)
            .Select(i => new VitalSnapshot(VitalCodes.SystolicBp, 80 - i, DateTimeOffset.UtcNow.AddMinutes(-i)))
            .ToList();
        var risk = scorer.CalculateRisk("p1", vitals);
        risk.ShouldBeInRange(0.0, 1.0);
    }

    [Fact]
    public void EnhancedRiskScorer_uses_most_recent_reading_by_effective()
    {
        var options = Options.Create(new RiskScorerOptions());
        var scorer = new EnhancedRiskScorer(options);
        var now = DateTimeOffset.UtcNow;
        var vitals = new List<VitalSnapshot>
        {
            new(VitalCodes.SystolicBp, 120, now.AddMinutes(-5)),
            new(VitalCodes.SystolicBp, 85, now)
        };
        scorer.CalculateRisk("p1", vitals).ShouldBeGreaterThanOrEqualTo(0.8);
    }

    [Fact]
    public void EnhancedRiskScorer_heart_rate_8867_4_code()
    {
        var options = Options.Create(new RiskScorerOptions { TachycardiaThreshold = 100 });
        var scorer = new EnhancedRiskScorer(options);
        var vitals = new List<VitalSnapshot>
        {
            new("8867-4", 110, DateTimeOffset.UtcNow)
        };
        scorer.CalculateRisk("p1", vitals).ShouldBeGreaterThan(0);
    }
}
