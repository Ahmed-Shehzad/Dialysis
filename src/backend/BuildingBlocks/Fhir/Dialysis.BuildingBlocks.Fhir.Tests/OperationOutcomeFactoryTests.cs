using Hl7.Fhir.Model;
using Shouldly;
using Xunit;

namespace Dialysis.BuildingBlocks.Fhir.Tests;

public sealed class OperationOutcomeFactoryTests
{
    [Fact]
    public void Notfound_Produces_Notfound_Issue()
    {
        var outcome = OperationOutcomeFactory.NotFound("Patient", "123");

        outcome.Issue.ShouldHaveSingleItem();
        outcome.Issue[0].Code.ShouldBe(OperationOutcome.IssueType.NotFound);
        outcome.Issue[0].Severity.ShouldBe(OperationOutcome.IssueSeverity.Error);
        outcome.Issue[0].Diagnostics.ShouldContain("Patient/123");
    }

    [Fact]
    public void Forbidden_Produces_Forbidden_Issue()
    {
        var outcome = OperationOutcomeFactory.Forbidden("consent denied");

        outcome.Issue[0].Code.ShouldBe(OperationOutcome.IssueType.Forbidden);
        outcome.Issue[0].Diagnostics.ShouldBe("consent denied");
    }

    [Fact]
    public void Fromexception_Sets_Error_Severity_And_Exception_Code()
    {
        var outcome = OperationOutcomeFactory.FromException(new InvalidOperationException("boom"));

        outcome.Issue[0].Code.ShouldBe(OperationOutcome.IssueType.Exception);
        outcome.Issue[0].Severity.ShouldBe(OperationOutcome.IssueSeverity.Error);
        outcome.Issue[0].Diagnostics.ShouldBe("boom");
    }

    [Fact]
    public void Badrequest_Maps_All_Severities()
    {
        var outcome = OperationOutcomeFactory.BadRequest(
        [
            new FhirError("required", "missing", FhirIssueSeverity.Warning),
            new FhirError("structure", "bad shape", FhirIssueSeverity.Fatal),
        ]);

        outcome.Issue.Count.ShouldBe(2);
        outcome.Issue[0].Severity.ShouldBe(OperationOutcome.IssueSeverity.Warning);
        outcome.Issue[1].Severity.ShouldBe(OperationOutcome.IssueSeverity.Fatal);
    }
}
