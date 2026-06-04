using System.Security.Cryptography;
using Dialysis.BuildingBlocks.Documents.Storage;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIE.Documents.Domain;
using Dialysis.HIE.Documents.Invoicing;
using Dialysis.HIE.Documents.Ports;
using Microsoft.Extensions.Logging;

namespace Dialysis.HIE.Documents.Consumers;

/// <summary>
/// Consumes <see cref="DialysisInvoiceReadyIntegrationEvent"/> from EHR.Billing, renders the
/// itemised AcroForm invoice PDF, stores the bytes, and indexes them as a HIE
/// <see cref="DocumentReference"/> (Kind <c>invoice</c>). The chairside then previews / fills /
/// downloads it through the existing documents endpoints.
///
/// Idempotent: the document id is the originating <see cref="DialysisInvoiceReadyIntegrationEvent.ChargeId"/>,
/// so re-delivery finds the existing row and exits without re-rendering.
/// </summary>
public sealed class OnDialysisInvoiceReady : IConsumer<DialysisInvoiceReadyIntegrationEvent>
{
    /// <summary>Document <see cref="DocumentReference.Kind"/> used for generated invoices.</summary>
    public const string InvoiceKind = "invoice";

    private readonly InvoicePdfBuilder _builder;
    private readonly IDocumentBlobStore _blobs;
    private readonly IDocumentReferenceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OnDialysisInvoiceReady> _logger;

    /// <summary>Creates the consumer.</summary>
    public OnDialysisInvoiceReady(InvoicePdfBuilder builder,
        IDocumentBlobStore blobs,
        IDocumentReferenceRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<OnDialysisInvoiceReady> logger)
    {
        _builder = builder;
        _blobs = blobs;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(ConsumeContext<DialysisInvoiceReadyIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var ct = context.CancellationToken;
        var message = context.Message;

        // The charge id doubles as the document id, so re-delivery is a no-op.
        var existing = await _repository.FindAsync(message.ChargeId, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            _logger.LogDebug("Invoice document for charge {ChargeId} already exists; skipping.", message.ChargeId);
            return;
        }

        var bytes = await _builder.BuildAsync(message, ct).ConfigureAwait(false);
        var storageRef = await _blobs.SaveAsync(message.ChargeId, "application/pdf", bytes, ct).ConfigureAwait(false);
        var hash = Convert.ToHexString(SHA256.HashData(bytes));

        var document = new DocumentReference(
            id: message.ChargeId,
            patientId: message.PatientId,
            kind: InvoiceKind,
            title: $"Invoice {message.InvoiceNumber}",
            mimeType: "application/pdf",
            storageRef: storageRef,
            contentHash: hash,
            size: bytes.LongLength,
            source: DocumentReferenceSource.Billing,
            createdAtUtc: message.IssueDateUtc,
            createdBy: null,
            category: message.SessionId.ToString("D"),
            languageCode: null,
            hasAcroForms: true,
            hasJavascript: false);
        _repository.Add(document);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Rendered invoice {InvoiceNumber} ({DocumentId}) for patient {PatientId} session {SessionId}.",
            message.InvoiceNumber, document.Id, message.PatientId, message.SessionId);
    }
}
