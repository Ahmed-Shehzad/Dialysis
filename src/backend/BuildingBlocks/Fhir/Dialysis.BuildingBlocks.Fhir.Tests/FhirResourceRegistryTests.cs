using Hl7.Fhir.Model;
using Shouldly;
using Xunit;

namespace Dialysis.BuildingBlocks.Fhir.Tests;

public sealed class FhirResourceRegistryTests
{
    [Fact]
    public void Registerreader_Marks_Read_Support()
    {
        var registry = new FhirResourceRegistry();

        registry.RegisterReader<Patient>();

        registry.Entries.ShouldContainKey("Patient");
        registry.Entries["Patient"].SupportsRead.ShouldBeTrue();
        registry.Entries["Patient"].SupportsSearch.ShouldBeFalse();
    }

    [Fact]
    public void Registersearcher_Marks_Search_Support_Without_Clobbering_Read()
    {
        var registry = new FhirResourceRegistry();

        registry.RegisterReader<Patient>();
        registry.RegisterSearcher<Patient>();

        registry.Entries["Patient"].SupportsRead.ShouldBeTrue();
        registry.Entries["Patient"].SupportsSearch.ShouldBeTrue();
    }

    [Fact]
    public void Unknown_Resource_Type_Has_No_Dispatcher()
    {
        var registry = new FhirResourceRegistry();

        registry.TryGetReadDispatcher("Patient", out _).ShouldBeFalse();
    }

    [Fact]
    public void Registerprofile_Appends_Profile_Url()
    {
        var registry = new FhirResourceRegistry();

        registry.RegisterReader<Patient>();
        registry.RegisterProfile<Patient>("http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient");

        registry.Entries["Patient"].SupportedProfiles.ShouldHaveSingleItem();
    }
}
