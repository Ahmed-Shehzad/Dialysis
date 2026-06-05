using Dialysis.BuildingBlocks.Fhir.Terminology;
using Hl7.Fhir.Model;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.Tests.Terminology;

/// <summary>
/// Coverage for the governed platform terminology catalog backing $validate-code / $translate /
/// $expand / $lookup over the lab + imaging value sets and concept maps.
/// </summary>
public sealed class DialysisTerminologyCatalogTests
{
    private readonly DialysisTerminologyCatalog _catalog = new();

    [Fact]
    public async Task Validate_Code_Accepts_A_Loinc_In_The_Lab_Panel_Async()
    {
        var result = await _catalog.Service.ValidateCodeAsync(
            DialysisTerminologyCatalog.DialysisLabPanelValueSet, "2160-0", DialysisTerminologyCatalog.LoincSystem, CancellationToken.None);

        ((FhirBoolean)result.Parameter.Single(p => p.Name == "result").Value!).Value.ShouldBe(true);
    }

    [Fact]
    public async Task Validate_Code_Rejects_A_Code_Not_In_The_Value_Set_Async()
    {
        var result = await _catalog.Service.ValidateCodeAsync(
            DialysisTerminologyCatalog.DialysisLabPanelValueSet, "0000-0", DialysisTerminologyCatalog.LoincSystem, CancellationToken.None);

        ((FhirBoolean)result.Parameter.Single(p => p.Name == "result").Value!).Value.ShouldBe(false);
    }

    [Fact]
    public async Task Translate_Maps_A_Local_Lab_Code_To_Loinc_Async()
    {
        var result = await _catalog.Service.TranslateAsync(
            DialysisTerminologyCatalog.LocalLabToLoincConceptMap, DialysisTerminologyCatalog.LocalLabSystem, "CR", CancellationToken.None);

        var match = result.Parameter.Single(p => p.Name == "match");
        var concept = (Coding)match.Part.Single(p => p.Name == "concept").Value!;
        concept.System.ShouldBe(DialysisTerminologyCatalog.LoincSystem);
        concept.Code.ShouldBe("2160-0");
    }

    [Fact]
    public async Task Expand_Returns_The_Lab_Panel_Concepts_Async()
    {
        var expanded = await _catalog.Service.ExpandAsync(
            DialysisTerminologyCatalog.DialysisLabPanelValueSet, new Dictionary<string, string>(), CancellationToken.None);

        expanded.Expansion.ShouldNotBeNull();
        expanded.Expansion.Contains.Count.ShouldBe(8);
        expanded.Expansion.Contains.ShouldContain(c => c.Code == "2823-3");
    }

    [Fact]
    public async Task Lookup_Returns_The_Display_For_A_Rad_Lex_Code_Async()
    {
        var result = await _catalog.Service.LookupAsync(
            DialysisTerminologyCatalog.RadLexSystem, "RID39055", CancellationToken.None);

        ((FhirString)result.Parameter.Single(p => p.Name == "display").Value!).Value.ShouldBe("Patent vascular access");
    }

    [Fact]
    public void Governance_Catalog_Lists_Every_Canonical_Resource()
    {
        _catalog.Resources.Count.ShouldBe(5);
        _catalog.Resources.ShouldContain(r => r.ResourceType == "ValueSet" && r.Url == DialysisTerminologyCatalog.DialysisLabPanelValueSet);
        _catalog.Resources.ShouldContain(r => r.ResourceType == "ConceptMap");
        _catalog.Resources.ShouldAllBe(r => r.Version == "1.0.0" && r.Status == "Active");
    }
}
