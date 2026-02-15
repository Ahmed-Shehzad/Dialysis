namespace Dialysis.Documents.Services;

/// <summary>Stores PDF in FHIR as Binary + DocumentReference and retrieves content.</summary>
public interface IDocumentStore
{
    /// <summary>Store PDF and create Binary + DocumentReference. Returns DocumentReference ID.</summary>
    Task<DocumentStoreResult> StoreAsync(
        byte[] pdfContent,
        string patientId,
        string documentTypeLoinc,
        string? encounterId,
        string? description,
        CancellationToken cancellationToken = default);

    /// <summary>Get DocumentReference by ID.</summary>
    Task<DocumentReferenceInfo?> GetAsync(string documentReferenceId, CancellationToken cancellationToken = default);

    /// <summary>Get PDF binary content by DocumentReference ID.</summary>
    Task<byte[]?> GetContentAsync(string documentReferenceId, CancellationToken cancellationToken = default);
}

public sealed record DocumentStoreResult(string DocumentReferenceId, string BinaryId);

public sealed record DocumentReferenceInfo(
    string Id,
    string PatientId,
    string DocumentType,
    string? EncounterId,
    DateTime? Date,
    string? Description);
