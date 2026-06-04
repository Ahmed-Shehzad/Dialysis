using Dialysis.HIE.OpenEhr.Archetypes.Declarative;
using Hl7.Fhir.Model;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.OpenEhr;

public sealed class ResourcePathTests
{
    [Fact]
    public void Evaluate_Returns_Null_When_Source_Is_Null() => ResourcePath.Evaluate(null, "Code.Coding[0].Code").ShouldBeNull();

    [Fact]
    public void Evaluate_Walks_Property_Chain()
    {
        var patient = new Patient { BirthDate = "1980-01-15" };
        ResourcePath.Evaluate(patient, "BirthDate").ShouldBe("1980-01-15");
    }

    [Fact]
    public void Evaluate_Resolves_First_List_Element()
    {
        var patient = new Patient
        {
            Name = { new HumanName { Family = "Doe", Given = ["Jane"] } },
        };
        ResourcePath.Evaluate(patient, "Name[0].Family").ShouldBe("Doe");
        ResourcePath.Evaluate(patient, "Name[0].Given[0]").ShouldBe("Jane");
    }

    [Fact]
    public void Evaluate_Returns_Null_When_Index_Out_Of_Range()
    {
        var patient = new Patient();
        ResourcePath.Evaluate(patient, "Name[0].Family").ShouldBeNull();
    }

    [Fact]
    public void Evaluate_Maps_Across_Wildcard_And_Flattens()
    {
        var observation = new Observation
        {
            Interpretation =
            {
                new CodeableConcept("http://hl7.org/fhir/v2/0078", "N"),
                new CodeableConcept("http://hl7.org/fhir/v2/0078", "A"),
            },
        };
        var result = ResourcePath.Evaluate(observation, "Interpretation[?].Coding[?].Code");
        result.ShouldBeAssignableTo<IReadOnlyList<object?>>();
        var list = (IReadOnlyList<object?>)result!;
        list.Count.ShouldBe(2);
        list.ShouldContain("N");
        list.ShouldContain("A");
    }

    [Fact]
    public void Evaluate_Subtype_Cast_Returns_Null_On_Mismatch()
    {
        var observation = new Observation { Value = new FhirString("65 kg") };
        ResourcePath.Evaluate(observation, "Value as Quantity.Value").ShouldBeNull();
        ResourcePath.Evaluate(observation, "Value as FhirString.Value").ShouldBe("65 kg");
    }

    [Fact]
    public void Evaluate_Subtype_Cast_Returns_Value_On_Match()
    {
        var observation = new Observation { Value = new Quantity(75.5m, "kg", "http://unitsofmeasure.org") };
        ResourcePath.Evaluate(observation, "Value as Quantity.Value").ShouldBe(75.5m);
        ResourcePath.Evaluate(observation, "Value as Quantity.Unit").ShouldBe("kg");
    }

    [Fact]
    public void Evaluate_Enum_Is_Normalised_To_Its_String_Name()
    {
        var observation = new Observation { Status = ObservationStatus.Final };
        ResourcePath.Evaluate(observation, "Status").ShouldBe("Final");
    }

    [Fact]
    public void Evaluate_Empty_Wildcard_Yields_Null()
    {
        var observation = new Observation();
        ResourcePath.Evaluate(observation, "Note[?].Text").ShouldBeNull();
    }
}
