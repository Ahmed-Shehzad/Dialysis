using Dialysis.BuildingBlocks.Fhir.CdaBridge;
using Hl7.Fhir.Model;
using Shouldly;
using Xunit;

namespace Dialysis.BuildingBlocks.Fhir.Tests.CdaBridge;

/// <summary>
/// Outbound FHIR → C-CDA emitter coverage, plus a CDA → FHIR → CDA → FHIR round-trip proving the
/// six sections survive both directions.
/// </summary>
public sealed class DefaultFhirToCdaMapperTests
{
    private static readonly DefaultFhirToCdaMapper _emitter = new();
    private static readonly DefaultCdaToFhirMapper _parser = new();

    [Fact]
    public void Emits_Header_And_Record_Target_From_Patient()
    {
        var bundle = new Bundle { Type = Bundle.BundleType.Document };
        var patient = new Patient { Id = "p1", Gender = AdministrativeGender.Female, BirthDate = "1985-12-10" };
        patient.Identifier.Add(new Identifier("http://hl7.org/fhir/sid/us-ssn", "999-22-1111"));
        patient.Name.Add(new HumanName { Family = "Lovelace" }.WithGiven("Ada"));
        patient.Telecom.Add(new ContactPoint { System = ContactPoint.ContactPointSystem.Phone, Value = "+49-30-1234567" });
        bundle.Entry.Add(new Bundle.EntryComponent { Resource = patient });

        var xml = _emitter.Map(bundle);

        xml.ShouldContain("34133-9");
        xml.ShouldContain("Lovelace");
        xml.ShouldContain("Ada");
        xml.ShouldContain("administrativeGenderCode");
        xml.ShouldContain("19851210");
        xml.ShouldContain("tel:+49-30-1234567");
    }

    [Fact]
    public void Emits_A_Section_Per_Resource_Kind_Present()
    {
        var bundle = new Bundle { Type = Bundle.BundleType.Document };
        bundle.Entry.Add(Entry(new Patient { Id = "p1" }));
        bundle.Entry.Add(Entry(new Condition { Code = Concept("http://hl7.org/fhir/sid/icd-10-cm", "N18.6", "ESRD") }));
        bundle.Entry.Add(Entry(new Immunization { VaccineCode = Concept("http://hl7.org/fhir/sid/cvx", "158", "Influenza") }));

        var xml = _emitter.Map(bundle);

        xml.ShouldContain("11450-4"); // Problems
        xml.ShouldContain("11369-6"); // Immunizations
        xml.ShouldNotContain("48765-2"); // Allergies — none in bundle, so no section
    }

    [Fact]
    public void Round_Trips_All_Six_Sections_Through_Both_Directions()
    {
        // CDA → FHIR (the rich fixture) → CDA → FHIR. The second parse must still carry one
        // resource of every section kind, proving the emitter reproduced each section in a shape
        // the parser re-reads.
        var first = _parser.Map(CdaFixtures.FullCcd);
        var cda = _emitter.Map(first);
        var second = _parser.Map(cda);

        var resources = second.Entry.Select(e => e.Resource).ToList();
        resources.OfType<Condition>().ShouldHaveSingleItem();
        resources.OfType<AllergyIntolerance>().ShouldHaveSingleItem();
        resources.OfType<MedicationStatement>().ShouldHaveSingleItem();
        resources.OfType<Immunization>().ShouldHaveSingleItem();
        resources.OfType<Observation>().Count().ShouldBe(2);

        // Spot-check that coded values survived the round-trip.
        resources.OfType<Condition>().Single().Code.Coding.ShouldContain(c => c.Code == "N18.6");
        resources.OfType<Immunization>().Single().VaccineCode.Coding.ShouldContain(c => c.Code == "158");
        var creatinine = resources.OfType<Observation>().Single(o => o.Code.Coding.Any(c => c.Code == "2160-0"));
        (creatinine.Value as Quantity)!.Value.ShouldBe(8.4m);
    }

    [Fact]
    public void Round_Trip_Preserves_Patient_Demographics()
    {
        var first = _parser.Map(CdaFixtures.FullCcd);
        var second = _parser.Map(_emitter.Map(first));

        var patient = second.Entry.Select(e => e.Resource).OfType<Patient>().Single();
        patient.Name.Single().Family.ShouldBe("Lovelace");
        patient.Gender.ShouldBe(AdministrativeGender.Female);
        patient.BirthDate.ShouldBe("1985-12-10");
        patient.Address.Single().City.ShouldBe("Berlin");
    }

    [Fact]
    public void Null_Bundle_Throws()
    {
        Should.Throw<ArgumentNullException>(() => _emitter.Map(null!));
    }

    private static Bundle.EntryComponent Entry(Resource resource) => new() { Resource = resource };

    private static CodeableConcept Concept(string system, string code, string display) =>
        new() { Coding = [new Coding(system, code, display)] };
}
