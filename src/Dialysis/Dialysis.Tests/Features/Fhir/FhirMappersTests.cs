using Dialysis.Gateway.Features.Fhir;
using Shouldly;
using Xunit;

namespace Dialysis.Tests.Features.Fhir;

public sealed class FhirMappersTests
{
    [Fact]
    public void FromFhirPatientJson_extracts_id_from_top_level_id()
    {
        var json = """
            {"resourceType":"Patient","id":"patient-001"}
            """;

        var (logicalId, familyName, givenNames, birthDate) = FhirMappers.FromFhirPatientJson(json);

        logicalId.ShouldBe("patient-001");
        familyName.ShouldBeNull();
        givenNames.ShouldBeNull();
        birthDate.ShouldBeNull();
    }

    [Fact]
    public void FromFhirPatientJson_extracts_from_identifier_with_urn_dialysis_patient()
    {
        var json = """
            {"resourceType":"Patient","identifier":[{"system":"urn:dialysis:patient","value":"patient-002"}]}
            """;

        var (logicalId, _, _, _) = FhirMappers.FromFhirPatientJson(json);

        logicalId.ShouldBe("patient-002");
    }

    [Fact]
    public void FromFhirPatientJson_extracts_name_and_birthDate()
    {
        var json = """
            {"resourceType":"Patient","id":"p1","name":[{"family":"Doe","given":["Jane"]}],"birthDate":"1990-03-15"}
            """;

        var (logicalId, familyName, givenNames, birthDate) = FhirMappers.FromFhirPatientJson(json);

        logicalId.ShouldBe("p1");
        familyName.ShouldBe("Doe");
        givenNames.ShouldBe("Jane");
        birthDate.ShouldBe(new DateTime(1990, 3, 15));
    }

    [Fact]
    public void FromFhirPatientJson_throws_when_no_id_or_identifier()
    {
        // Valid FHIR Patient JSON with name but without id or identifier
        var json = """
            {"resourceType":"Patient","name":[{"family":"Doe"}]}
            """;

        var ex = Should.Throw<ArgumentException>(() => FhirMappers.FromFhirPatientJson(json));
        ex.Message.ShouldContain("identifier");
    }

    [Fact]
    public void FromFhirPatientJson_throws_on_invalid_json()
    {
        var json = "{ invalid json }";

        Should.Throw<System.Text.Json.JsonException>(() => FhirMappers.FromFhirPatientJson(json));
    }
}
