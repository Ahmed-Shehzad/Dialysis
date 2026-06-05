using Dialysis.BuildingBlocks.Fhir.DeIdentification;
using Hl7.Fhir.Model;
using Shouldly;
using Xunit;

namespace Dialysis.BuildingBlocks.Fhir.Tests.DeIdentification;

/// <summary>
/// Coverage for the HIPAA Safe Harbor de-identifier: it clones (never mutates the source), drops the
/// narrative on every resource, strips the direct identifiers, and generalizes dates to year across the
/// resource types the bulk-data feeders emit.
/// </summary>
public sealed class SafeHarborDeIdentifierTests
{
    private readonly SafeHarborDeIdentifier _deId = new();

    [Fact]
    public void Patient_Loses_Name_Identifiers_And_Date_Is_Year_Only()
    {
        var source = new Patient
        {
            Id = "p1",
            Name = [new HumanName { Family = "Doe", Given = ["Jane"] }],
            Identifier = [new Identifier("urn:dialysis:mrn", "MRN-123")],
            Telecom = [new ContactPoint { Value = "+1-555-0100" }],
            BirthDate = "1980-07-14",
            Text = new Narrative { Status = Narrative.NarrativeStatus.Generated, Div = "<div>Jane Doe, 14 Jul 1980</div>" },
        };

        var result = (Patient)_deId.Apply(source, DeIdentificationProfile.SafeHarbor);

        result.Name.ShouldBeEmpty();
        result.Identifier.ShouldBeEmpty();
        result.Telecom.ShouldBeEmpty();
        result.BirthDate.ShouldBe("1980");
        result.Text.ShouldBeNull();

        // Source is untouched (operates on a clone).
        source.Name.ShouldNotBeEmpty();
        source.BirthDate.ShouldBe("1980-07-14");
    }

    [Fact]
    public void Allergy_Intolerance_Loses_Notes_Identifiers_And_Recorded_Date_Is_Year_Only()
    {
        var source = new AllergyIntolerance
        {
            Id = "a1",
            Identifier = [new Identifier("urn:dialysis:allergy", "AL-9")],
            Note = [new Annotation { Text = "Patient reported on call from home number." }],
            RecordedDate = "2025-03-22",
            Text = new Narrative { Status = Narrative.NarrativeStatus.Generated, Div = "<div>peanut allergy</div>" },
        };

        var result = (AllergyIntolerance)_deId.Apply(source, DeIdentificationProfile.SafeHarbor);

        result.Note.ShouldBeEmpty();
        result.Identifier.ShouldBeEmpty();
        result.RecordedDate.ShouldBe("2025");
        result.Text.ShouldBeNull();
    }

    [Fact]
    public void Procedure_Narrative_Is_Dropped_And_Performed_Date_Is_Year_Only()
    {
        var source = new Procedure
        {
            Id = "pr1",
            Performed = new FhirDateTime("2025-11-02T09:30:00Z"),
            Note = [new Annotation { Text = "free text" }],
            Text = new Narrative { Status = Narrative.NarrativeStatus.Generated, Div = "<div>HD session</div>" },
        };

        var result = (Procedure)_deId.Apply(source, DeIdentificationProfile.SafeHarbor);

        result.Text.ShouldBeNull();
        result.Note.ShouldBeEmpty();
        ((FhirDateTime)result.Performed!).Value.ShouldBe("2025");
    }
}
