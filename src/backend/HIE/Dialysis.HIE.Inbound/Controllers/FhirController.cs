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
public sealed class FhirController : ControllerBase
{
    private readonly InboundIngestionService _ingestion;
    private readonly IPatientIndex _patientIndex;
    private readonly ILogger<FhirController> _logger;
    /// <summary>
    /// FHIR R4 REST surface. Returns native <c>application/fhir+json</c> bodies (spec compliance) so it does
    /// NOT wrap responses in the HATEOAS envelope used by admin endpoints elsewhere in this host.
    /// </summary>
    public FhirController(InboundIngestionService ingestion,
        IPatientIndex patientIndex,
        ILogger<FhirController> logger)
    {
        _ingestion = ingestion;
        _patientIndex = patientIndex;
        _logger = logger;
    }
    private static readonly FhirJsonDeserializer _parser = new(new DeserializerSettings().UsingMode(DeserializationMode.Recoverable));

    [HttpPost("Bundle")]
    [Consumes("application/fhir+json", "application/json")]
    public async Task<IActionResult> ReceiveBundleAsync(CancellationToken cancellationToken)
    {
        var partnerId = Request.Headers["X-HIE-Partner"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(partnerId))
            return FhirResult(BadRequest(), MissingPartnerOutcome());

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        Resource resource;
        try
        {
            resource = _parser.Deserialize<Resource>(body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inbound FHIR parse error");
            return FhirResult(UnprocessableEntity(), ParseErrorOutcome(ex.Message));
        }

        var outcome = await _ingestion.IngestAsync(partnerId, resource, cancellationToken).ConfigureAwait(false);
        var status = outcome.Issue.Any(i => i.Severity is OperationOutcome.IssueSeverity.Error or OperationOutcome.IssueSeverity.Fatal)
            ? StatusCodes.Status422UnprocessableEntity
            : StatusCodes.Status200OK;
        return FhirResult(StatusCode(status), outcome);
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
        var matches = await _patientIndex.MatchAsync(mrn, family, given, dob, take: 20, cancellationToken).ConfigureAwait(false);

        var bundle = new Bundle { Type = Bundle.BundleType.Searchset, Total = matches.Count };
        foreach (var m in matches)
        {
            var patient = new Patient { Id = m.ExternalLogicalId };
            if (!string.IsNullOrWhiteSpace(m.MedicalRecordNumber))
            {
                patient.Identifier.Add(new Identifier
                {
                    System = Core.Coding.CodeSystems.MrnIdentifier,
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

        return FhirResult(Ok(), bundle);
    }

    private static IActionResult FhirResult(IActionResult fallbackStatus, Resource resource)
    {
        var json = resource.ToJson();

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
