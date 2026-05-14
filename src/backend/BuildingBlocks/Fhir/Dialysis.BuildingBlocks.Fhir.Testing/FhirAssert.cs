using Dialysis.BuildingBlocks.Fhir.Serialization;
using Hl7.Fhir.Model;
using Xunit;
using Xunit.Sdk;

namespace Dialysis.BuildingBlocks.Fhir.Testing;

public static class FhirAssert
{
    private static readonly FhirJsonSerializerProvider _serializer = new();

    /// <summary>
    /// Assert two FHIR resources are equal by canonical JSON comparison.
    /// </summary>
    public static void ResourceEqual(Resource expected, Resource actual)
    {
        var expectedJson = _serializer.Serialize(expected, pretty: true);
        var actualJson = _serializer.Serialize(actual, pretty: true);
        if (!string.Equals(expectedJson, actualJson, StringComparison.Ordinal))
        {
            throw new XunitException(
                $"FHIR resources differ.\nExpected:\n{expectedJson}\nActual:\n{actualJson}");
        }
    }

    public static void OperationOutcomeContains(OperationOutcome outcome, OperationOutcome.IssueType code)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        Assert.Contains(outcome.Issue, issue => issue.Code == code);
    }
}
