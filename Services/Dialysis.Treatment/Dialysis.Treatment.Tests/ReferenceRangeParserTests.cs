using Dialysis.Treatment.Application.Domain.Hl7;
using Dialysis.Treatment.Application.Domain.ValueObjects;

using Shouldly;

namespace Dialysis.Treatment.Tests;

public sealed class ReferenceRangeParserTests
{
    [Theory]
    [InlineData("20-400", 20, 400)]
    [InlineData("90 - 140", 90, 140)]
    [InlineData("0.5-1.5", 0.5, 1.5)]
    public void TryParse_BoundedRange_ReturnsParsedResult(string raw, double expectedLower, double expectedUpper)
    {
        ReferenceRangeInfo result = ReferenceRangeParser.TryParse(raw).ShouldNotBeNull();
        result.Lower.ShouldBe(expectedLower);
        result.Upper.ShouldBe(expectedUpper);
        result.Kind.ShouldBe(ReferenceRangeKind.Bounded);
    }

    [Theory]
    [InlineData("> 20", 20)]
    [InlineData(">20", 20)]
    [InlineData("> 0.5", 0.5)]
    public void TryParse_GreaterThanLower_ReturnsParsedResult(string raw, double expectedLower)
    {
        ReferenceRangeInfo result = ReferenceRangeParser.TryParse(raw).ShouldNotBeNull();
        result.Lower.ShouldBe(expectedLower);
        result.Upper.ShouldBeNull();
        result.Kind.ShouldBe(ReferenceRangeKind.GreaterThanLower);
    }

    [Theory]
    [InlineData("< 400", 400)]
    [InlineData("<400", 400)]
    [InlineData("< 1.5", 1.5)]
    public void TryParse_LessThanUpper_ReturnsParsedResult(string raw, double expectedUpper)
    {
        ReferenceRangeInfo result = ReferenceRangeParser.TryParse(raw).ShouldNotBeNull();
        result.Lower.ShouldBeNull();
        result.Upper.ShouldBe(expectedUpper);
        result.Kind.ShouldBe(ReferenceRangeKind.LessThanUpper);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("N")]
    [InlineData("normal")]
    public void TryParse_InvalidOrEmpty_ReturnsNull(string? raw) => ReferenceRangeParser.TryParse(raw).ShouldBeNull();
}
