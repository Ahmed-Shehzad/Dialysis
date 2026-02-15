using System.Text;
using FhirCore.Gateway.Fhir;

namespace FhirCore.Gateway.Validation;

public sealed class OperationOutcomeWriter
{
    public async Task WriteAsync(HttpResponse response, IReadOnlyList<string> errors)
    {
        response.StatusCode = StatusCodes.Status400BadRequest;
        response.ContentType = "application/fhir+json";

        var outcome = new
        {
            resourceType = "OperationOutcome",
            issue = errors.Select(e => new
            {
                severity = "error",
                code = "invalid",
                diagnostics = e
            }).ToArray()
        };

        var json = FhirJson.Serialize(outcome);
        await response.WriteAsync(json, Encoding.UTF8);
    }
}
