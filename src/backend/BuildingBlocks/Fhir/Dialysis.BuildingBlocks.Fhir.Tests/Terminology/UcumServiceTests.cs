using Dialysis.BuildingBlocks.Fhir.Terminology;
using Shouldly;
using Xunit;

namespace Dialysis.BuildingBlocks.Fhir.Tests.Terminology;

public sealed class UcumServiceTests
{
    private static readonly UcumService _sut = new();

    [Fact]
    public void Tryparseunit_Accepts_Valid_Ucum_Expression()
    {
        _sut.TryParseUnit("kg").ShouldBeTrue();
        _sut.TryParseUnit("mg/dL").ShouldBeTrue();
        _sut.TryParseUnit("mm[Hg]").ShouldBeTrue();
    }

    [Fact]
    public void Tryparseunit_Rejects_Garbage()
    {
        _sut.TryParseUnit("not-a-unit").ShouldBeFalse();
        _sut.TryParseUnit("").ShouldBeFalse();
    }

    [Fact]
    public void Trycanonicalize_Reduces_Kg_To_Grams()
    {
        _sut.TryCanonicalize(70m, "kg", out var canonical).ShouldBeTrue();
        canonical.Value.ShouldBe(70000m);
        canonical.CanonicalUnit.ShouldBe("g");
    }

    [Fact]
    public void Tryconvert_Kg_To_G_Returns_Thousand_Multiplier()
    {
        _sut.TryConvert(1m, "kg", "g", out var converted).ShouldBeTrue();
        converted.ShouldBe(1000m);
    }

    [Fact]
    public void Tryconvert_Rejects_Incommensurable_Units() => _sut.TryConvert(1m, "kg", "s", out _).ShouldBeFalse();

    [Fact]
    public void Trycompare_Treats_Equivalent_Quantities_As_Equal()
    {
        _sut.TryCompare(70m, "kg", 70_000m, "g", out var comparison).ShouldBeTrue();
        comparison.ShouldBe(0);
    }

    [Fact]
    public void Trycompare_Orders_Lighter_Before_Heavier()
    {
        _sut.TryCompare(70m, "kg", 75_000m, "g", out var comparison).ShouldBeTrue();
        comparison.ShouldBe(-1);
    }
}
