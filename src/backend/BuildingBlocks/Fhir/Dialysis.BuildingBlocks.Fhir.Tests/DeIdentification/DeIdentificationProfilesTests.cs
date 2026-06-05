using Dialysis.BuildingBlocks.Fhir.DeIdentification;
using Hl7.Fhir.Model;
using Shouldly;
using Xunit;

namespace Dialysis.BuildingBlocks.Fhir.Tests.DeIdentification;

/// <summary>
/// Coverage for the Limited Data Set and Custom de-identification profiles (Safe Harbor has its own
/// suite). LDS (§164.514(e)) retains full dates + city/state/ZIP while stripping direct identifiers;
/// Custom is rule-driven and defaults to the strict Safe Harbor settings.
/// </summary>
public sealed class DeIdentificationProfilesTests
{
    [Fact]
    public void Limited_Data_Set_Keeps_Full_Dates_And_City_State_Zip_But_Drops_Names_And_Street()
    {
        var source = new Patient
        {
            Name = [new HumanName { Family = "Doe", Given = ["Jane"] }],
            Identifier = [new Identifier("urn:dialysis:mrn", "MRN-123")],
            Telecom = [new ContactPoint { Value = "+1-555-0100" }],
            BirthDate = "1980-07-14",
            Address =
            [
                new Address
                {
                    Line = ["123 Main St"],
                    City = "Springfield",
                    District = "Sangamon",
                    State = "IL",
                    PostalCode = "62704",
                },
            ],
        };

        var result = (Patient)new SafeHarborDeIdentifier().Apply(source, DeIdentificationProfile.LimitedDataSet);

        // Direct identifiers stripped.
        result.Name.ShouldBeEmpty();
        result.Telecom.ShouldBeEmpty();
        result.Identifier.ShouldBeEmpty();
        // Dates retained in full (the LDS distinction from Safe Harbor).
        result.BirthDate.ShouldBe("1980-07-14");
        // Geography kept to city/state/ZIP; street line + district dropped.
        var address = result.Address.ShouldHaveSingleItem();
        address.Line.ShouldBeEmpty();
        address.District.ShouldBeNull();
        address.City.ShouldBe("Springfield");
        address.State.ShouldBe("IL");
        address.PostalCode.ShouldBe("62704");
    }

    [Fact]
    public void Limited_Data_Set_Keeps_The_Observation_Effective_Date_In_Full()
    {
        var source = new Observation { Effective = new FhirDateTime("2025-03-22T09:30:00Z") };

        var result = (Observation)new SafeHarborDeIdentifier().Apply(source, DeIdentificationProfile.LimitedDataSet);

        ((FhirDateTime)result.Effective!).Value.ShouldBe("2025-03-22T09:30:00Z");
    }

    [Fact]
    public void Custom_Defaults_To_Safe_Harbor_When_Unconfigured()
    {
        var source = new Patient
        {
            Name = [new HumanName { Family = "Doe" }],
            BirthDate = "1980-07-14",
            Address = [new Address { City = "Springfield", State = "IL" }],
        };

        var result = (Patient)new SafeHarborDeIdentifier().Apply(source, DeIdentificationProfile.Custom);

        result.Name.ShouldBeEmpty();
        result.BirthDate.ShouldBe("1980");       // generalized to year, like Safe Harbor
        result.Address.ShouldBeEmpty();          // geography removed, like Safe Harbor
    }

    [Fact]
    public void Custom_Honors_Relaxed_Rules()
    {
        var rules = new CustomDeIdentificationRules
        {
            RemoveNarrative = false,
            RemoveDirectIdentifiers = false,
            GeneralizeDatesToYear = false,
            Address = AddressGranularity.Full,
        };
        var source = new Patient
        {
            Name = [new HumanName { Family = "Doe" }],
            BirthDate = "1980-07-14",
            Address = [new Address { Line = ["123 Main St"], City = "Springfield" }],
            Text = new Narrative { Status = Narrative.NarrativeStatus.Generated, Div = "<div>Jane Doe</div>" },
        };

        var result = (Patient)new SafeHarborDeIdentifier(rules).Apply(source, DeIdentificationProfile.Custom);

        result.Name.ShouldNotBeEmpty();
        result.BirthDate.ShouldBe("1980-07-14");
        result.Address.ShouldHaveSingleItem().Line.ShouldContain("123 Main St");
        result.Text.ShouldNotBeNull();
    }
}
