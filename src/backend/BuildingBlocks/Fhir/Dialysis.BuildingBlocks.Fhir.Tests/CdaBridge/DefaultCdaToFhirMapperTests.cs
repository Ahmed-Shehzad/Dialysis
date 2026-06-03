using Dialysis.BuildingBlocks.Fhir.CdaBridge;
using Hl7.Fhir.Model;
using Shouldly;
using Xunit;

namespace Dialysis.BuildingBlocks.Fhir.Tests.CdaBridge;

/// <summary>
/// Inbound C-CDA → FHIR parser coverage: full patient demographics plus one resource from each
/// of the six supported sections, and the parser's null-soft / malformed-input behaviour.
/// </summary>
public sealed class DefaultCdaToFhirMapperTests
{
    private static readonly DefaultCdaToFhirMapper _mapper = new();

    private static Bundle MapFull() => _mapper.Map(CdaFixtures.FullCcd);

    [Fact]
    public void Parses_Patient_Demographics()
    {
        var patient = MapFull().Entry.Select(e => e.Resource).OfType<Patient>().Single();

        patient.Identifier.Count.ShouldBe(2);
        patient.Identifier[0].Value.ShouldBe("MRN-7788");
        var name = patient.Name.Single();
        name.Family.ShouldBe("Lovelace");
        name.Given.ShouldBe(["Ada", "Marie"]);
        name.Prefix.ShouldContain("Dr");
        patient.Gender.ShouldBe(AdministrativeGender.Female);
        patient.BirthDate.ShouldBe("1985-12-10");
        patient.Telecom.ShouldContain(t => t.System == ContactPoint.ContactPointSystem.Phone && t.Value == "+49-30-1234567");
        patient.Telecom.ShouldContain(t => t.System == ContactPoint.ContactPointSystem.Email && t.Value == "ada@example.org");
        var address = patient.Address.Single();
        address.City.ShouldBe("Berlin");
        address.PostalCode.ShouldBe("10115");
        address.Line.ShouldContain("123 Dialysis Way");
    }

    [Fact]
    public void Bundle_Is_A_Document_Anchored_By_Composition()
    {
        var bundle = MapFull();
        bundle.Type.ShouldBe(Bundle.BundleType.Document);
        var composition = bundle.Entry[0].Resource.ShouldBeOfType<Composition>();
        composition.Section.Count.ShouldBe(6);
    }

    [Fact]
    public void Parses_Problem_Into_Condition()
    {
        var condition = MapFull().Entry.Select(e => e.Resource).OfType<Condition>().Single();
        condition.Code.Coding.ShouldContain(c => c.Code == "N18.6" && c.System == "http://hl7.org/fhir/sid/icd-10-cm");
        (condition.Onset as FhirDateTime)!.Value.ShouldBe("2024-01-15");
        condition.ClinicalStatus.Coding.ShouldContain(c => c.Code == "active");
    }

    [Fact]
    public void Parses_Allergy_Substance_Into_Allergy_Intolerance()
    {
        var allergy = MapFull().Entry.Select(e => e.Resource).OfType<AllergyIntolerance>().Single();
        allergy.Code.Coding.ShouldContain(c => c.Code == "7980" && c.Display == "Penicillin");
        allergy.Patient.Reference.ShouldStartWith("Patient/");
    }

    [Fact]
    public void Parses_Medication_Into_Medication_Statement_With_Period()
    {
        var statement = MapFull().Entry.Select(e => e.Resource).OfType<MedicationStatement>().Single();
        (statement.Medication as CodeableConcept)!.Coding.ShouldContain(c => c.Code == "855332");
        var period = statement.Effective.ShouldBeOfType<Period>();
        period.Start.ShouldBe("2025-01-01");
        period.End.ShouldBe("2025-12-31");
    }

    [Fact]
    public void Parses_Result_And_Vital_Into_Categorised_Observations()
    {
        var observations = MapFull().Entry.Select(e => e.Resource).OfType<Observation>().ToList();
        observations.Count.ShouldBe(2);

        var creatinine = observations.Single(o => o.Code.Coding.Any(c => c.Code == "2160-0"));
        creatinine.Category.ShouldContain(c => c.Coding.Any(x => x.Code == "laboratory"));
        var qty = creatinine.Value.ShouldBeOfType<Quantity>();
        qty.Value.ShouldBe(8.4m);
        qty.Unit.ShouldBe("mg/dL");

        var bp = observations.Single(o => o.Code.Coding.Any(c => c.Code == "8480-6"));
        bp.Category.ShouldContain(c => c.Coding.Any(x => x.Code == "vital-signs"));
    }

    [Fact]
    public void Parses_Immunization_With_Lot_Number()
    {
        var immunization = MapFull().Entry.Select(e => e.Resource).OfType<Immunization>().Single();
        immunization.VaccineCode.Coding.ShouldContain(c => c.Code == "158" && c.System == "http://hl7.org/fhir/sid/cvx");
        immunization.Status.ShouldBe(Immunization.ImmunizationStatusCodes.Completed);
        immunization.LotNumber.ShouldBe("LOT-2025-A");
        (immunization.Occurrence as FhirDateTime)!.Value.ShouldBe("2025-10-01");
    }

    [Fact]
    public void Header_Only_Document_Maps_Patient_Without_Sections()
    {
        var bundle = _mapper.Map(CdaFixtures.HeaderOnly);
        var patient = bundle.Entry.Select(e => e.Resource).OfType<Patient>().Single();
        patient.Name.Single().Family.ShouldBe("Hopper");
        bundle.Entry.Select(e => e.Resource).OfType<Condition>().ShouldBeEmpty();
        bundle.Entry[0].Resource.ShouldBeOfType<Composition>().Section.ShouldBeEmpty();
    }

    [Fact]
    public void Malformed_Xml_Throws_Format_Exception()
    {
        Should.Throw<FormatException>(() => _mapper.Map("<ClinicalDocument><unclosed>"));
    }

    [Fact]
    public void Empty_Input_Throws_Argument_Exception()
    {
        Should.Throw<ArgumentException>(() => _mapper.Map("   "));
    }
}
