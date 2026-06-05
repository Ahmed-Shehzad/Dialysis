using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.BuildingBlocks.Fhir.DeIdentification;
using Shouldly;
using Xunit;

namespace Dialysis.BuildingBlocks.Fhir.Tests.BulkData;

/// <summary>Coverage for the export de-identification profile resolver (fail-closed on unknown values).</summary>
public sealed class ExportDeIdentificationTests
{
    [Theory]
    [InlineData("SafeHarbor")]
    [InlineData("safe-harbor")]
    [InlineData("true")]
    [InlineData("1")]
    public void Resolve_Profile_Maps_Safe_Harbor_Aliases(string requested)
    {
        ExportDeIdentification.ResolveProfile(requested).ShouldBe(DeIdentificationProfile.SafeHarbor);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_Profile_Returns_Null_When_Not_Requested(string? requested)
    {
        ExportDeIdentification.ResolveProfile(requested).ShouldBeNull();
    }

    [Fact]
    public void Resolve_Profile_Throws_On_An_Unknown_Value()
    {
        Should.Throw<FormatException>(() => ExportDeIdentification.ResolveProfile("scramble-everything"));
    }
}
