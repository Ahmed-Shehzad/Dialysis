namespace Dialysis.EHealthGateway.Services;

/// <summary>Platform adapter for eHealth document exchange (ePA, DMP, eHIR).</summary>
public interface IEHealthPlatformAdapter
{
    /// <summary>Platform identifier: epa, dmp, ehir.</summary>
    string PlatformId { get; }

    /// <summary>Push document to eHealth platform on behalf of patient.</summary>
    /// <param name="documentContent">PDF or CDA bytes.</param>
    /// <param name="patientIdentifier">Platform-specific patient ID (e.g. KVNR, INS).</param>
    /// <param name="documentType">LOINC or document type code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<EHealthUploadResult> UploadAsync(
        byte[] documentContent,
        string patientIdentifier,
        string? documentType,
        CancellationToken cancellationToken = default);

    /// <summary>Query patient documents from eHealth platform.</summary>
    /// <param name="patientIdentifier">Platform-specific patient ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<EHealthDocumentInfo>> ListDocumentsAsync(
        string patientIdentifier,
        CancellationToken cancellationToken = default);
}

public sealed record EHealthUploadResult(bool Success, string? DocumentId, string? Error);

public sealed record EHealthDocumentInfo(string Id, string? DocumentType, DateTimeOffset? Date, string? Description);
