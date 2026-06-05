using Dialysis.BuildingBlocks.Fhir.Terminology;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.Tests.Terminology;

/// <summary>
/// Coverage for the FHIR-free <see cref="IDialysisCodeValidator"/> facade that coding producers use to
/// gate / normalise codes against the governed catalog without touching the raw Parameters shape.
/// </summary>
public sealed class DialysisCodeValidatorTests
{
    private readonly IDialysisCodeValidator _validator = new DialysisCodeValidator(new DialysisTerminologyCatalog());

    [Fact]
    public async Task Validate_Accepts_A_Governed_Loinc_Async()
    {
        var result = await _validator.ValidateAsync(
            DialysisTerminologyCatalog.DialysisLabPanelValueSet, "2160-0", DialysisTerminologyCatalog.LoincSystem, CancellationToken.None);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_Rejects_An_Ungoverned_Code_Async()
    {
        var result = await _validator.ValidateAsync(
            DialysisTerminologyCatalog.DialysisLabPanelValueSet, "0000-0", DialysisTerminologyCatalog.LoincSystem, CancellationToken.None);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Translate_Maps_Local_To_Loinc_Async()
    {
        var result = await _validator.TranslateAsync(
            DialysisTerminologyCatalog.LocalLabToLoincConceptMap, DialysisTerminologyCatalog.LocalLabSystem, "CR", CancellationToken.None);

        result.ShouldNotBeNull();
        result!.TargetSystem.ShouldBe(DialysisTerminologyCatalog.LoincSystem);
        result.TargetCode.ShouldBe("2160-0");
    }

    [Fact]
    public async Task Translate_Returns_Null_For_An_Unmapped_Source_Code_Async()
    {
        var result = await _validator.TranslateAsync(
            DialysisTerminologyCatalog.LocalLabToLoincConceptMap, DialysisTerminologyCatalog.LocalLabSystem, "ZZZ", CancellationToken.None);

        result.ShouldBeNull();
    }
}
