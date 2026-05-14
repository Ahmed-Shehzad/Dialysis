using Asp.Versioning;
using Dialysis.HIE.Inbound.Ingestion;
using Dialysis.HIE.Inbound.Ports;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Dialysis.HIE.Inbound.Controllers;

/// <summary>
/// FHIR R4 REST surface. Returns native <c>application/fhir+json</c> bodies (spec compliance) so it does
/// NOT wrap responses in the HATEOAS envelope used by admin endpoints elsewhere in this host.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/fhir")]
[Authorize]
[Produces("application/fhir+json")]
public sealed class FhirController(
    InboundIngestionService ingestion,
    IPatientIndex patientIndex,
    ILogger<FhirController> logger) : ControllerBase
{
    private static readonly FhirJsonParser _parser = new();
    private static readonly FhirJsonSerializer _serializer = new();

    [HttpPost("Bundle")]
    [Consumes("application/fhir+json", "application/json")]
    public async Task<IActionResult> ReceiveBundleAsync(CancellationToken cancellationToken)
    {
        var partnerId = Request.Headers["X-HIE-Partner"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(partnerId))
            return await FhirResultAsync(BadRequest(), MissingPartnerOutcome(), cancellationToken).ConfigureAwait(false);

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        Resource resource;
        try
        {
            resource = await _parser.ParseAsync<Resource>(body);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Inbound FHIR parse error");
            return await FhirResultAsync(UnprocessableEntity(), ParseErrorOutcome(ex.Message), cancellationToken).ConfigureAwait(false);
        }

        var outcome = await ingestion.IngestAsync(partnerId, resource, cancellationToken).ConfigureAwait(false);
        var status = outcome.Issue.Any(i => i.Severity is OperationOutcome.IssueSeverity.Error or OperationOutcome.IssueSeverity.Fatal)
            ? StatusCodes.Status422UnprocessableEntity
            : StatusCodes.Status200OK;
        return await FhirResultAsync(StatusCode(status), outcome, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("Patient/$match")]
    public async Task<IActionResult> PatientMatchAsync(
        [FromQuery] string? mrn,
        [FromQuery] string? family,
        [FromQuery] string? given,
        [FromQuery] string? birthdate,
        CancellationToken cancellationToken)
    {
        DateOnly? dob = null;
        if (!string.IsNullOrWhiteSpace(birthdate) && DateOnly.TryParse(birthdate, out var parsed))
            dob = parsed;
        var matches = await patientIndex.MatchAsync(mrn, family, given, dob, take: 20, cancellationToken).ConfigureAwait(false);

        var bundle = new Bundle { Type = Bundle.BundleType.Searchset, Total = matches.Count };
        foreach (var m in matches)
        {
            var patient = new Patient { Id = m.ExternalLogicalId };
            if (!string.IsNullOrWhiteSpace(m.MedicalRecordNumber))
            {
                patient.Identifier.Add(new Identifier
                {
                    System = Dialysis.HIE.Core.Coding.CodeSystems.MrnIdentifier,
                    Value = m.MedicalRecordNumber,
                });
            }
            if (!string.IsNullOrWhiteSpace(m.FamilyName) || !string.IsNullOrWhiteSpace(m.GivenName))
            {
                patient.Name.Add(new HumanName
                {
                    Family = m.FamilyName,
                    Given = m.GivenName is null ? [] : [m.GivenName],
                });
            }
            if (m.DateOfBirth is { } dobValue)
                patient.BirthDate = dobValue.ToString("yyyy-MM-dd");
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"Patient/{m.ExternalLogicalId}",
                Resource = patient,
            });
        }

        return await FhirResultAsync(Ok(), bundle, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IActionResult> FhirResultAsync(IActionResult fallbackStatus, Resource resource, CancellationToken cancellationToken)
    {
        var json = await _serializer.SerializeToStringAsync(resource).ConfigureAwait(false);
        var statusCode = fallbackStatus switch
        {
            StatusCodeResult sc => sc.StatusCode,
            ObjectResult or => or.StatusCode ?? StatusCodes.Status200OK,
            _ => StatusCodes.Status200OK
        };
        return new ContentResult
        {
            Content = json,
            ContentType = "application/fhir+json",
            StatusCode = statusCode,
        };
    }

    private static OperationOutcome MissingPartnerOutcome() => new()
    {
        Issue =
        {
            new OperationOutcome.IssueComponent
            {
                Severity = OperationOutcome.IssueSeverity.Error,
                Code = OperationOutcome.IssueType.Required,
                Diagnostics = "Missing X-HIE-Partner header identifying the sending partner.",
            },
        },
    };

    private static OperationOutcome ParseErrorOutcome(string message) => new()
    {
        Issue =
        {
            new OperationOutcome.IssueComponent
            {
                Severity = OperationOutcome.IssueSeverity.Error,
                Code = OperationOutcome.IssueType.Structure,
                Diagnostics = message,
            },
        },
    };
}
