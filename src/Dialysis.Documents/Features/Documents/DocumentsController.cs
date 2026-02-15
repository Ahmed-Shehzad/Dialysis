using Asp.Versioning;
using Dialysis.Documents.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Documents.Features.Documents;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/documents")]
[Authorize(Policy = "Read")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IPdfGenerator _pdfGenerator;
    private readonly IPdfTemplateFiller _templateFiller;
    private readonly IBundleToPdfConverter _bundleConverter;
    private readonly IDocumentStore _documentStore;

    public DocumentsController(
        IPdfGenerator pdfGenerator,
        IPdfTemplateFiller templateFiller,
        IBundleToPdfConverter bundleConverter,
        IDocumentStore documentStore)
    {
        _pdfGenerator = pdfGenerator;
        _templateFiller = templateFiller;
        _bundleConverter = bundleConverter;
        _documentStore = documentStore;
    }

    /// <summary>Generate PDF from FHIR data. Template: session-summary, patient-summary, measure-report.</summary>
    [HttpPost("generate-pdf")]
    [Authorize(Policy = "Read")]
    public async Task<IActionResult> GeneratePdf(
        [FromBody] GeneratePdfRequest? body = null,
        CancellationToken cancellationToken = default)
    {
        var template = body?.Template ?? "patient-summary";
        var patientId = body?.PatientId;
        var encounterId = body?.EncounterId;
        var resourceId = body?.ResourceId;
        Hl7.Fhir.Model.Bundle? bundle = null;

        if (body?.BundleJson != null)
        {
            try
            {
                bundle = new FhirJsonDeserializer().Deserialize<Hl7.Fhir.Model.Bundle>(body.BundleJson);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Invalid FHIR Bundle: {ex.Message}" });
            }
        }

        if (string.IsNullOrEmpty(patientId) && bundle == null && string.IsNullOrEmpty(resourceId))
        {
            return BadRequest(new { error = "Provide patientId, resourceId, or bundle" });
        }

        try
        {
            var pdf = await _pdfGenerator.GenerateAsync(template, patientId, encounterId, resourceId, bundle, cancellationToken);
            return File(pdf, "application/pdf", $"report-{template}-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Fill AcroForm PDF template with FHIR data or explicit mappings.</summary>
    [HttpPost("fill-template")]
    [Authorize(Policy = "Write")]
    public async Task<IActionResult> FillTemplate(
        [FromBody] FillTemplateRequest body,
        [FromQuery] bool includeScripts = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(body.TemplateId))
            return BadRequest(new { error = "templateId is required" });

        try
        {
            var pdf = await _templateFiller.FillAsync(
                body.TemplateId,
                body.PatientId,
                body.EncounterId,
                body.Mappings,
                includeScripts,
                cancellationToken);
            return File(pdf, "application/pdf", $"filled-{body.TemplateId}-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Convert FHIR Document Bundle or DocumentReference to PDF.</summary>
    [HttpPost("bundle-to-pdf")]
    [Authorize(Policy = "Read")]
    public async Task<IActionResult> BundleToPdf(
        [FromBody] BundleToPdfRequest? body = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(body?.DocumentReferenceId))
        {
            try
            {
                var pdf = await _bundleConverter.ConvertFromDocumentReferenceAsync(body.DocumentReferenceId, cancellationToken);
                return File(pdf, "application/pdf", $"document-{body.DocumentReferenceId}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        if (body?.BundleJson == null)
            return BadRequest(new { error = "Provide bundleJson or documentReferenceId" });

        Bundle bundle;
        try
        {
            bundle = new FhirJsonDeserializer().Deserialize<Hl7.Fhir.Model.Bundle>(body.BundleJson);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Invalid FHIR Bundle: {ex.Message}" });
        }

        try
        {
            var pdf = await _bundleConverter.ConvertAsync(bundle, cancellationToken);
            return File(pdf, "application/pdf", $"bundle-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Upload PDF and create FHIR Binary + DocumentReference.</summary>
    [HttpPost]
    [Authorize(Policy = "Write")]
    [Consumes("multipart/form-data", "application/json")]
    public async Task<IActionResult> UploadDocument(
        IFormFile? file = null,
        [FromForm] string? patientId = null,
        [FromForm] string? documentType = null,
        [FromForm] string? encounterId = null,
        [FromForm] string? description = null,
        [FromBody] UploadDocumentRequest? body = null,
        CancellationToken cancellationToken = default)
    {
        patientId ??= body?.PatientId;
        documentType ??= body?.DocumentType ?? "34117-2";
        encounterId ??= body?.EncounterId;
        description ??= body?.Description;

        byte[] pdfContent;
        if (file != null)
        {
            if (file.ContentType?.Contains("pdf", StringComparison.OrdinalIgnoreCase) != true)
                return BadRequest(new { error = "File must be application/pdf" });
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, cancellationToken);
            pdfContent = ms.ToArray();
        }
        else if (!string.IsNullOrEmpty(body?.Base64Data))
        {
            try
            {
                pdfContent = Convert.FromBase64String(body.Base64Data);
            }
            catch
            {
                return BadRequest(new { error = "Invalid base64Data" });
            }
        }
        else
        {
            return BadRequest(new { error = "Provide file (multipart) or base64Data (JSON)" });
        }

        if (string.IsNullOrWhiteSpace(patientId))
            return BadRequest(new { error = "patientId is required" });

        try
        {
            var result = await _documentStore.StoreAsync(pdfContent, patientId, documentType, encounterId, description, cancellationToken);
            return Ok(new { documentReferenceId = result.DocumentReferenceId, binaryId = result.BinaryId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Get DocumentReference metadata by ID.</summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "Read")]
    public async Task<IActionResult> GetDocument(string id, CancellationToken cancellationToken = default)
    {
        var info = await _documentStore.GetAsync(id, cancellationToken);
        if (info == null) return NotFound();
        return Ok(new { id = info.Id, patientId = info.PatientId, documentType = info.DocumentType, encounterId = info.EncounterId, date = info.Date, description = info.Description });
    }

    /// <summary>Get PDF binary content by DocumentReference ID.</summary>
    [HttpGet("{id}/content")]
    [Authorize(Policy = "Read")]
    public async Task<IActionResult> GetDocumentContent(string id, CancellationToken cancellationToken = default)
    {
        var content = await _documentStore.GetContentAsync(id, cancellationToken);
        if (content == null || content.Length == 0) return NotFound();
        return File(content, "application/pdf", $"document-{id}.pdf");
    }
}

public sealed record GeneratePdfRequest(
    string? Template,
    string? PatientId,
    string? EncounterId,
    string? ResourceId,
    string? BundleJson);

public sealed record FillTemplateRequest(
    string TemplateId,
    string? PatientId,
    string? EncounterId,
    IReadOnlyDictionary<string, string>? Mappings);

public sealed record BundleToPdfRequest(
    string? BundleJson,
    string? DocumentReferenceId);

public sealed record UploadDocumentRequest(
    string? PatientId,
    string? DocumentType,
    string? EncounterId,
    string? Description,
    string? Base64Data);
