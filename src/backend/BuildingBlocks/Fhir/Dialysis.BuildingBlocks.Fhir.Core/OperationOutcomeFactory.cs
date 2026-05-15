using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir;

/// <summary>
/// Builds <see cref="OperationOutcome"/> instances for common error categories.
/// FHIR clients expect this shape for any non-success response.
/// </summary>
public static class OperationOutcomeFactory
{
    public static OperationOutcome FromException(Exception ex) => new()
    {
        Issue =
        [
            new OperationOutcome.IssueComponent
            {
                Severity = OperationOutcome.IssueSeverity.Error,
                Code = OperationOutcome.IssueType.Exception,
                Diagnostics = ex.Message,
            },
        ],
    };

    public static OperationOutcome NotFound(string resourceType, string id) => new()
    {
        Issue =
        [
            new OperationOutcome.IssueComponent
            {
                Severity = OperationOutcome.IssueSeverity.Error,
                Code = OperationOutcome.IssueType.NotFound,
                Diagnostics = $"{resourceType}/{id} not found.",
            },
        ],
    };

    public static OperationOutcome Forbidden(string reason) => new()
    {
        Issue =
        [
            new OperationOutcome.IssueComponent
            {
                Severity = OperationOutcome.IssueSeverity.Error,
                Code = OperationOutcome.IssueType.Forbidden,
                Diagnostics = reason,
            },
        ],
    };

    public static OperationOutcome BadRequest(IEnumerable<FhirError> errors) => new()
    {
        Issue = [.. errors.Select(e => new OperationOutcome.IssueComponent
        {
            Severity = MapSeverity(e.Severity),
            Code = OperationOutcome.IssueType.Invalid,
            Diagnostics = e.Diagnostics,
            Details = new CodeableConcept { Coding = [new Coding(system: null, code: e.Code)], Text = e.Diagnostics },
        })],
    };

    public static OperationOutcome NotSupported(string reason) => new()
    {
        Issue =
        [
            new OperationOutcome.IssueComponent
            {
                Severity = OperationOutcome.IssueSeverity.Error,
                Code = OperationOutcome.IssueType.NotSupported,
                Diagnostics = reason,
            },
        ],
    };

    private static OperationOutcome.IssueSeverity MapSeverity(FhirIssueSeverity severity) => severity switch
    {
        FhirIssueSeverity.Information => OperationOutcome.IssueSeverity.Information,
        FhirIssueSeverity.Warning => OperationOutcome.IssueSeverity.Warning,
        FhirIssueSeverity.Fatal => OperationOutcome.IssueSeverity.Fatal,
        _ => OperationOutcome.IssueSeverity.Error,
    };
}
