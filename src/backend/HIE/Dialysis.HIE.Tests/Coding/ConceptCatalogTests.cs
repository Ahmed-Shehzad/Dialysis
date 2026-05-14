using Dialysis.HIE.Core.Coding;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.Coding;

public sealed class ConceptCatalogTests
{
    private static ConceptCatalog Make_Sut() => new(
    [
        new(ClinicalConcepts.RenalDialysis, CodeSystems.SnomedCt, "265764009", "Renal dialysis"),
        new(ClinicalConcepts.SubsequentEvaluationNote, CodeSystems.Loinc, "11506-3", "Subsequent evaluation note"),
    ]);

    [Fact]
    public void Get_Returns_Concept_With_System_And_Code()
    {
        var concept = Make_Sut().Get(ClinicalConcepts.RenalDialysis);

        var coding = concept.Coding.ShouldHaveSingleItem();
        coding.System.ShouldBe(CodeSystems.SnomedCt);
        coding.Code.ShouldBe("265764009");
        coding.Display.ShouldBe("Renal dialysis");
        concept.Text.ShouldBe("Renal dialysis");
    }

    [Fact]
    public void Get_Throws_For_Unknown_Concept()
    {
        Should.Throw<KeyNotFoundException>(() => Make_Sut().Get("nonexistent"));
    }

    [Fact]
    public void Tryget_Returns_Null_For_Unknown_Concept()
    {
        Make_Sut().TryGet("nonexistent").ShouldBeNull();
    }

    [Fact]
    public void Updatedisplay_Replaces_Display_On_Subsequent_Get()
    {
        var catalog = Make_Sut();
        catalog.UpdateDisplay(ClinicalConcepts.RenalDialysis, "Haemodialysis procedure");

        var concept = catalog.Get(ClinicalConcepts.RenalDialysis);
        concept.Coding[0].Display.ShouldBe("Haemodialysis procedure");
        concept.Text.ShouldBe("Haemodialysis procedure");
    }

    [Fact]
    public void Tryget_Returns_Defensive_Copy()
    {
        var catalog = Make_Sut();
        var first = catalog.Get(ClinicalConcepts.RenalDialysis);
        first.Coding[0].Display = "mutated";

        var second = catalog.Get(ClinicalConcepts.RenalDialysis);
        second.Coding[0].Display.ShouldBe("Renal dialysis");
    }
}
