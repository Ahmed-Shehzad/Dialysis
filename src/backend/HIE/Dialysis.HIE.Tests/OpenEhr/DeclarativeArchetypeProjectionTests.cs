using System.Text.Json;
using Dialysis.HIE.OpenEhr.Archetypes.Declarative;
using Hl7.Fhir.Model;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.OpenEhr;

public sealed class DeclarativeArchetypeProjectionTests
{
    [Fact]
    public void Catalog_Has_Three_Shipped_Definitions()
    {
        var definitions = ArchetypeMappingCatalog.LoadEmbeddedDefinitions(
            typeof(ArchetypeMappingCatalog).Assembly);
        definitions.Count.ShouldBe(3);
        var ids = definitions.Select(d => d.ArchetypeId).ToHashSet();
        ids.ShouldContain("openEHR-DEMOGRAPHIC-PERSON.person.v1");
        ids.ShouldContain("openEHR-EHR-COMPOSITION.haemodialysis_session.v1");
        ids.ShouldContain("openEHR-EHR-OBSERVATION.lab_test_result.v1");
    }

    [Fact]
    public void Patient_Projection_Captures_Identifier_Name_And_Demographics()
    {
        var sut = MakeProjection<Patient>("openEHR-DEMOGRAPHIC-PERSON.person.v1");
        var patient = new Patient
        {
            Identifier = { new Identifier("urn:mrn", "M-12345") { Type = new CodeableConcept("urn:type", "MR") } },
            Name = { new HumanName { Family = "Doe", Given = ["Jane"] } },
            BirthDate = "1980-01-15",
            Gender = AdministrativeGender.Female,
        };

        var json = sut.Project(patient);
        var fields = Read_Fields(json);

        fields["identifier.issuer"].GetString().ShouldBe("urn:mrn");
        fields["identifier.value"].GetString().ShouldBe("M-12345");
        fields["identifier.type"].GetString().ShouldBe("MR");
        fields["name.family"].GetString().ShouldBe("Doe");
        fields["name.given"].GetString().ShouldBe("Jane");
        fields["birth.date"].GetString().ShouldBe("1980-01-15");
        fields["gender"].GetString().ShouldBe("Female");
    }

    [Fact]
    public void Observation_Projection_Captures_Loinc_Code_And_Quantity()
    {
        var sut = MakeProjection<Observation>("openEHR-EHR-OBSERVATION.lab_test_result.v1");
        var observation = new Observation
        {
            Code = new CodeableConcept("http://loinc.org", "29463-7", "Body weight", null),
            Value = new Quantity(75.5m, "kg", "http://unitsofmeasure.org"),
            Effective = new FhirDateTime("2026-06-03T12:00:00Z"),
            Status = ObservationStatus.Final,
            Interpretation = { new CodeableConcept("http://hl7.org/fhir/v2/0078", "N") },
        };

        var json = sut.Project(observation);
        var fields = Read_Fields(json);

        fields["code.code"].GetString().ShouldBe("29463-7");
        fields["code.system"].GetString().ShouldBe("http://loinc.org");
        fields["code.display"].GetString().ShouldBe("Body weight");
        fields["value.magnitude"].GetDecimal().ShouldBe(75.5m);
        fields["value.unit"].GetString().ShouldBe("kg");
        fields["effective_time"].GetString().ShouldBe("2026-06-03T12:00:00Z");
        fields["status"].GetString().ShouldBe("Final");
        fields["interpretation"][0].GetString().ShouldBe("N");
    }

    [Fact]
    public void Procedure_Projection_Captures_Code_Period_And_Outcome()
    {
        var sut = MakeProjection<Procedure>("openEHR-EHR-COMPOSITION.haemodialysis_session.v1");
        var procedure = new Procedure
        {
            Code = new CodeableConcept("http://snomed.info/sct", "302497006", "Haemodialysis", null),
            Status = EventStatus.Completed,
            Performed = new Period
            {
                Start = "2026-06-03T08:00:00Z",
                End = "2026-06-03T12:00:00Z",
            },
            Outcome = new CodeableConcept("urn:outcome", "successful"),
            Note = { new Annotation { Text = new Markdown("uneventful session") } },
        };

        var json = sut.Project(procedure);
        var fields = Read_Fields(json);

        fields["procedure.code"].GetString().ShouldBe("302497006");
        fields["procedure.system"].GetString().ShouldBe("http://snomed.info/sct");
        fields["status"].GetString().ShouldBe("Completed");
        fields["period.start"].GetString().ShouldBe("2026-06-03T08:00:00Z");
        fields["period.end"].GetString().ShouldBe("2026-06-03T12:00:00Z");
        fields["outcome"].GetString().ShouldBe("successful");
        fields["notes"][0].GetString().ShouldBe("uneventful session");
    }

    [Fact]
    public void Projection_Skips_Null_Fields_From_The_Output()
    {
        // An Observation with only a code — every other field path returns null and the
        // serialised payload omits those keys.
        var sut = MakeProjection<Observation>("openEHR-EHR-OBSERVATION.lab_test_result.v1");
        var observation = new Observation
        {
            Code = new CodeableConcept("http://loinc.org", "29463-7"),
        };
        var fields = Read_Fields(sut.Project(observation));

        fields.ContainsKey("code.code").ShouldBeTrue();
        fields.ContainsKey("value.magnitude").ShouldBeFalse();
        fields.ContainsKey("status").ShouldBeFalse();
    }

    private static IArchetypeProjectionWrapper<TResource> MakeProjection<TResource>(string archetypeId)
        where TResource : Resource
    {
        var definition = ArchetypeMappingCatalog
            .LoadEmbeddedDefinitions(typeof(ArchetypeMappingCatalog).Assembly)
            .Single(d => d.ArchetypeId == archetypeId);
        return new ProjectionAdapter<TResource>(new DeclarativeArchetypeProjection<TResource>(definition));
    }

    private static Dictionary<string, JsonElement> Read_Fields(string json)
    {
        var doc = JsonDocument.Parse(json);
        var fields = doc.RootElement.GetProperty("fields");
        return fields.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
    }

    private interface IArchetypeProjectionWrapper<in TResource> where TResource : Resource
    {
        string Project(TResource resource);
    }

    private sealed class ProjectionAdapter<TResource>(DeclarativeArchetypeProjection<TResource> inner)
        : IArchetypeProjectionWrapper<TResource>
        where TResource : Resource
    {
        public string Project(TResource resource) => inner.Project(resource);
    }
}
