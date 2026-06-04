using System.Security.Cryptography;
using Dialysis.BuildingBlocks.Documents.Pdf.AcroForms;
using Dialysis.BuildingBlocks.Documents.Storage;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Documents.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Documents.Features.FillDocumentAcroForm;

/// <summary>
/// Server-side AcroForm fill — load the existing PDF, populate the supplied field values
/// through PDFsharp, and persist the new bytes as a revision on the same DocumentReference.
/// The whole point of doing this server-side is that the resulting bytes have the AcroForm
/// <c>/V</c> entries baked in, so a downstream PAdES signature actually covers the filled
/// values (rather than signing an empty form and trusting the viewer to keep them around).
/// </summary>
public sealed record FillDocumentAcroFormCommand : ICommand<FillDocumentAcroFormResult>, IPermissionedCommand
{
    /// <summary>
    /// Server-side AcroForm fill — load the existing PDF, populate the supplied field values
    /// through PDFsharp, and persist the new bytes as a revision on the same DocumentReference.
    /// The whole point of doing this server-side is that the resulting bytes have the AcroForm
    /// <c>/V</c> entries baked in, so a downstream PAdES signature actually covers the filled
    /// values (rather than signing an empty form and trusting the viewer to keep them around).
    /// </summary>
    public FillDocumentAcroFormCommand(Guid DocumentId,
        IReadOnlyDictionary<string, string> FieldValues)
    {
        this.DocumentId = DocumentId;
        this.FieldValues = FieldValues;
    }
    public string RequiredPermission => HiePermissions.DocumentsFill;
    public Guid DocumentId { get; init; }
    public IReadOnlyDictionary<string, string> FieldValues { get; init; }
    public void Deconstruct(out Guid DocumentId, out IReadOnlyDictionary<string, string> FieldValues)
    {
        DocumentId = this.DocumentId;
        FieldValues = this.FieldValues;
    }
}

/// <summary>
/// The fill result — the new document id (same row, revised bytes), the field names that
/// were applied, and any keys from the caller's dictionary that didn't exist in the PDF.
/// </summary>
public sealed record FillDocumentAcroFormResult
{
    /// <summary>
    /// The fill result — the new document id (same row, revised bytes), the field names that
    /// were applied, and any keys from the caller's dictionary that didn't exist in the PDF.
    /// </summary>
    public FillDocumentAcroFormResult(Guid DocumentId,
        IReadOnlyList<string> FilledFieldNames,
        IReadOnlyList<string> UnknownFields)
    {
        this.DocumentId = DocumentId;
        this.FilledFieldNames = FilledFieldNames;
        this.UnknownFields = UnknownFields;
    }
    public Guid DocumentId { get; init; }
    public IReadOnlyList<string> FilledFieldNames { get; init; }
    public IReadOnlyList<string> UnknownFields { get; init; }
    public void Deconstruct(out Guid DocumentId, out IReadOnlyList<string> FilledFieldNames, out IReadOnlyList<string> UnknownFields)
    {
        DocumentId = this.DocumentId;
        FilledFieldNames = this.FilledFieldNames;
        UnknownFields = this.UnknownFields;
    }
}

public sealed class FillDocumentAcroFormCommandHandler : ICommandHandler<FillDocumentAcroFormCommand, FillDocumentAcroFormResult>
{
    private readonly IDocumentReferenceRepository _repository;
    private readonly IDocumentBlobStore _blobs;
    private readonly IAcroFormProcessor _processor;
    private readonly IUnitOfWork _unitOfWork;
    public FillDocumentAcroFormCommandHandler(IDocumentReferenceRepository repository,
        IDocumentBlobStore blobs,
        IAcroFormProcessor processor,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _blobs = blobs;
        _processor = processor;
        _unitOfWork = unitOfWork;
    }
    public async Task<FillDocumentAcroFormResult> HandleAsync(FillDocumentAcroFormCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.FieldValues);

        var document = await _repository.FindAsync(request.DocumentId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Document {request.DocumentId} not found.");
        if (!string.Equals(document.MimeType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only PDF documents support AcroForm fill.");
        if (!document.HasAcroForms)
            throw new InvalidOperationException("Document does not contain an AcroForm — nothing to fill.");

        var bytes = await _blobs.ReadAsync(document.StorageRef, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Blob {document.StorageRef} is missing.");

        var fillResult = await _processor
            .FillFormValuesAsync(bytes, request.FieldValues, cancellationToken)
            .ConfigureAwait(false);

        var newRef = await _blobs.SaveAsync(Guid.CreateVersion7(), document.MimeType, fillResult.FilledBytes, cancellationToken)
            .ConfigureAwait(false);
        var newHash = Convert.ToHexString(SHA256.HashData(fillResult.FilledBytes));
        // Filling values doesn't add or remove macros; flags carry forward unchanged.
        document.Revise(newRef, newHash, fillResult.FilledBytes.LongLength, document.HasAcroForms, document.HasJavascript);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new FillDocumentAcroFormResult(document.Id, fillResult.FilledFieldNames, fillResult.UnknownFields);
    }
}
