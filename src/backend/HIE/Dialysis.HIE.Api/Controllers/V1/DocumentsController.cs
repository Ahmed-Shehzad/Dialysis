using Asp.Versioning;
using Dialysis.BuildingBlocks.Documents.Signing;
using Dialysis.BuildingBlocks.Fhir.AspNetCore.Audit;
using Dialysis.CQRS;
using Dialysis.HIE.Api.Hateoas;
using Dialysis.HIE.Documents.Domain;
using Dialysis.HIE.Documents.Features.DeleteDocument;
using Dialysis.HIE.Documents.Features.GetDocument;
using Dialysis.HIE.Documents.Features.GetDocumentBinary;
using Dialysis.HIE.Documents.Features.ListDocuments;
using Dialysis.HIE.Documents.Features.SignDocument;
using Dialysis.HIE.Documents.Features.UploadDocument;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIE.Api.Controllers.V1;

/// <summary>
/// Admin document-management surface. Indexes every clinical document (PDMS-produced,
/// HIE-received, admin-uploaded) into one browsable list with PDF preview, AcroForm fill
/// metadata, PAdES digital signing, and a soft-delete flow. Binary preview / AcroForms /
/// macros are served as-is and rendered by the SPA's pdfjs viewer; the server preserves
/// embedded JavaScript byte-for-byte and never executes it.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/documents")]
[Authorize]
public sealed class DocumentsController(ICqrsGateway cqrs) : ControllerBase
{
    [HttpGet]
    [PhiAccess("hie.documents.list")]
    [ProducesResponseType(typeof(ResourceEnvelope<IReadOnlyList<DocumentRow>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] Guid? patientId,
        [FromQuery] string? kind,
        [FromQuery] DocumentReferenceStatus? status,
        [FromQuery] DocumentReferenceSource? source,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var rows = await cqrs.SendQueryAsync<ListDocumentsQuery, IReadOnlyList<DocumentRow>>(
            new ListDocumentsQuery(patientId, kind, status, source, take), cancellationToken).ConfigureAwait(false);
        var self = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}";
        return Ok(new ResourceEnvelope<IReadOnlyList<DocumentRow>>(rows, [new LinkDto("self", self, "GET")]));
    }

    [HttpGet("{id:guid}")]
    [PhiAccess("hie.documents.read")]
    [ProducesResponseType(typeof(ResourceEnvelope<DocumentDetail>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var detail = await cqrs.SendQueryAsync<GetDocumentQuery, DocumentDetail?>(new GetDocumentQuery(id), cancellationToken)
            .ConfigureAwait(false);
        if (detail is null) return NotFound();
        var self = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}";
        var binaryHref = self + "/binary";
        return Ok(new ResourceEnvelope<DocumentDetail>(
            detail,
            [
                new LinkDto("self", self, "GET"),
                new LinkDto("hie:document:binary", binaryHref, "GET"),
                new LinkDto("hie:document:sign", self + "/sign", "POST"),
                new LinkDto("hie:document:delete", self, "DELETE"),
            ]));
    }

    [HttpGet("{id:guid}/binary")]
    [PhiAccess("hie.documents.binary.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBinaryAsync(Guid id, CancellationToken cancellationToken)
    {
        var binary = await cqrs.SendQueryAsync<GetDocumentBinaryQuery, DocumentBinary?>(
            new GetDocumentBinaryQuery(id), cancellationToken).ConfigureAwait(false);
        if (binary is null) return NotFound();
        return File(binary.Bytes, binary.MimeType);
    }

    [HttpPost]
    [PhiAccess("hie.documents.upload")]
    [ProducesResponseType(typeof(ResourceEnvelope<UploadedDocumentDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> UploadAsync([FromBody] UploadDocumentRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var command = new UploadDocumentCommand(
            body.PatientId, body.Kind, body.Title, body.MimeType, body.Base64Content,
            body.LanguageCode, body.Category, User.Identity?.Name);
        var id = await cqrs.SendCommandAsync<UploadDocumentCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        var location = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/v1.0/documents/{id}";
        return Created(location, new ResourceEnvelope<UploadedDocumentDto>(
            new UploadedDocumentDto(id),
            [new LinkDto("self", location, "GET")]));
    }

    [HttpPost("{id:guid}/sign")]
    [PhiAccess("hie.documents.sign")]
    [ProducesResponseType(typeof(ResourceEnvelope<UploadedDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SignAsync(Guid id, [FromBody] SignDocumentRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var command = new SignDocumentCommand(
            id,
            body.CertificateSource,
            body.UserId,
            body.Reason,
            body.Location,
            body.ContactInfo,
            body.Level ?? PadesConformance.B,
            body.TspCredentialId);
        var signedId = await cqrs.SendCommandAsync<SignDocumentCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        var self = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/v1.0/documents/{signedId}";
        return Ok(new ResourceEnvelope<UploadedDocumentDto>(
            new UploadedDocumentDto(signedId),
            [new LinkDto("self", self, "GET")]));
    }

    [HttpDelete("{id:guid}")]
    [PhiAccess("hie.documents.delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await cqrs.SendCommandAsync<DeleteDocumentCommand, Unit>(new DeleteDocumentCommand(id), cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    public sealed record UploadDocumentRequest(
        Guid PatientId,
        string Kind,
        string Title,
        string MimeType,
        string Base64Content,
        string? LanguageCode,
        string? Category);

    public sealed record SignDocumentRequest(
        PdfSigningCertificateSource CertificateSource,
        string? UserId,
        string? Reason,
        string? Location,
        string? ContactInfo,
        PadesConformance? Level = null,
        string? TspCredentialId = null);

    public sealed record UploadedDocumentDto(Guid DocumentId);
}
