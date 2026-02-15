using Asp.Versioning;
using Dialysis.ApiClients;
using Dialysis.EHealthGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.EHealthGateway.Features.EHealth;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/ehealth")]
[Authorize(Policy = "Write")]
public sealed class EHealthController : ControllerBase
{
    private readonly IEHealthPlatformAdapter _adapter;
    private readonly IConsentVerificationClient _consent;
    private readonly IDocumentContentResolver _documentResolver;

    public EHealthController(IEHealthPlatformAdapter adapter, IConsentVerificationClient consent, IDocumentContentResolver documentResolver)
    {
        _adapter = adapter;
        _consent = consent;
        _documentResolver = documentResolver;
    }

    /// <summary>Push document to eHealth platform (ePA, DMP). Requires document content and patient identifier (KVNR, INS).</summary>
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(
        [FromBody] EHealthUploadRequest body,
        [FromQuery] string? platform = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(body.PatientIdentifier))
            return BadRequest(new { error = "patientIdentifier is required" });
        if (string.IsNullOrWhiteSpace(body.DocumentReferenceId) && string.IsNullOrWhiteSpace(body.Base64Content))
            return BadRequest(new { error = "Provide documentReferenceId or base64Content" });

        var hasConsent = await _consent.HasConsentAsync("Consent", body.PatientIdentifier, "ehealth-upload", cancellationToken);
        if (!hasConsent)
            return StatusCode(403, new { error = "eHealth consent required for document upload" });

        byte[] content;
        if (!string.IsNullOrEmpty(body.DocumentReferenceId))
        {
            var resolved = await _documentResolver.ResolveAsync(body.DocumentReferenceId, cancellationToken);
            if (resolved == null)
                return NotFound(new { error = "DocumentReference not found; configure EHealth:DocumentsBaseUrl or EHealth:FhirBaseUrl" });
            content = resolved;
        }
        else
        {
            try
            {
                content = Convert.FromBase64String(body.Base64Content!);
            }
            catch
            {
                return BadRequest(new { error = "Invalid base64Content" });
            }
        }

        var result = await _adapter.UploadAsync(content, body.PatientIdentifier, body.DocumentType ?? "34117-2", cancellationToken);
        if (!result.Success)
            return StatusCode(502, new { error = result.Error, documentId = result.DocumentId });
        return Ok(new { documentId = result.DocumentId });
    }

    /// <summary>List patient documents from eHealth platform.</summary>
    [HttpGet("documents")]
    public async Task<IActionResult> ListDocuments(
        [FromQuery] string patientIdentifier,
        [FromQuery] string? platform = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(patientIdentifier))
            return BadRequest(new { error = "patientIdentifier is required" });

        var docs = await _adapter.ListDocumentsAsync(patientIdentifier, cancellationToken);
        return Ok(docs.Select(d => new { id = d.Id, documentType = d.DocumentType, date = d.Date, description = d.Description }));
    }
}

public sealed record EHealthUploadRequest(
    string PatientIdentifier,
    string? DocumentReferenceId,
    string? Base64Content,
    string? DocumentType);
