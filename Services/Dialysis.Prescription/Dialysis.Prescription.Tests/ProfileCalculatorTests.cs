using Dialysis.Prescription.Application.Domain.Services;
using Dialysis.Prescription.Application.Domain.ValueObjects;

using Shouldly;

namespace Dialysis.Prescription.Tests;

public sealed class ProfileCalculatorTests
{
    [Fact]
    public void EvaluateConstant_ReturnsFirstValue()
    {
        var profile = new ProfileDescriptor(ProfileType.Constant, [100m], null, null, null);
        ProfileCalculator.Evaluate(profile, 0, 240).ShouldBe(100m);
        ProfileCalculator.Evaluate(profile, 120, 240).ShouldBe(100m);
    }

    [Fact]
    public void EvaluateConstant_EmptyValues_ReturnsZero()
    {
        var profile = new ProfileDescriptor(ProfileType.Constant, [], null, null, null);
        ProfileCalculator.Evaluate(profile, 0, 240).ShouldBe(0);
    }

    [Theory]
    [InlineData(0, 240, 50)]
    [InlineData(120, 240, 75)]
    [InlineData(240, 240, 100)]
    public void EvaluateLinear_InterpolatesBetweenStartAndEnd(decimal time, decimal total, decimal expected)
    {
        var profile = new ProfileDescriptor(ProfileType.Linear, [50m, 100m], null, null, null);
        ProfileCalculator.Evaluate(profile, time, total).ShouldBe(expected);
    }

    [Fact]
    public void EvaluateLinear_WithTimeArray_UsesTimeArrayForNormalization()
    {
        var profile = new ProfileDescriptor(ProfileType.Linear, [10m, 90m], [0m, 100m], null, null);
        ProfileCalculator.Evaluate(profile, 50, 240).ShouldBe(50m);
    }

    [Fact]
    public void EvaluateExponential_WithHalfTime_DecaysCorrectly()
    {
        var profile = new ProfileDescriptor(ProfileType.Exponential, [100m, 25m], null, 30m, null);
        decimal atStart = ProfileCalculator.Evaluate(profile, 0, 240);
        decimal atEnd = ProfileCalculator.Evaluate(profile, 240, 240);
        atStart.ShouldBe(100m);
        atEnd.ShouldBeInRange(24m, 26m);
    }

    [Fact]
    public void EvaluateStep_ReturnsValueAtCurrentStep()
    {
        var profile = new ProfileDescriptor(ProfileType.Step, [10m, 20m, 30m], [0m, 60m, 120m], null, null);
        ProfileCalculator.Evaluate(profile, 0, 240).ShouldBe(10m);
        ProfileCalculator.Evaluate(profile, 30, 240).ShouldBe(10m);
        ProfileCalculator.Evaluate(profile, 60, 240).ShouldBe(20m);
        ProfileCalculator.Evaluate(profile, 90, 240).ShouldBe(20m);
        ProfileCalculator.Evaluate(profile, 120, 240).ShouldBe(30m);
    }

    [Fact]
    public void EvaluateVendor_ReturnsFirstValue()
    {
        var profile = new ProfileDescriptor(ProfileType.Vendor, [42m], null, null, "Acme^Model^Name");
        ProfileCalculator.Evaluate(profile, 0, 240).ShouldBe(42m);
        ProfileCalculator.Evaluate(profile, 120, 240).ShouldBe(42m);
    }
}
