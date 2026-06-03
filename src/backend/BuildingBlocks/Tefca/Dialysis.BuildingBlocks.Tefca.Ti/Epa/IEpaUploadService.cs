using Dialysis.BuildingBlocks.DataProtection.Consent;

namespace Dialysis.BuildingBlocks.Tefca.Ti.Epa;

/// <summary>
/// Uploads a document into the patient's elektronische Patientenakte (ePA). Gated by an
/// explicit PDSG consent check against <see cref="IPatientConsentGateway"/> (scope =
/// <see cref="ConsentScope.EpaDocument"/>); refused without consent.
///
/// Successful uploads return the ePA-assigned document id so the platform can record it on
/// the local `SessionReport` aggregate for traceability.
/// </summary>
public interface IEpaUploadService
{
    Task<EpaUploadResult> UploadAsync(EpaUploadRequest request, CancellationToken cancellationToken);
}

/// <param name="Language">
/// BCP-47 language tag of the document content (e.g. <c>de</c>, <c>en-US</c>). Carried into the
/// ePA metadata so the patient's record stores the document's language. Optional — defaults to
/// <c>de</c> when the caller doesn't resolve a language.
/// </param>
public sealed record EpaUploadRequest(
    Guid PatientId,
    string DocumentTitle,
    string MimeType,
    ReadOnlyMemory<byte> Content,
    string ActorSub,
    string Purpose,
    string? Language = null);

public sealed record EpaUploadResult(
    bool Succeeded,
    string? EpaDocumentId,
    string? FailureReason);

public interface IEpaDownloadService
{
    Task<EpaDownloadResult> DownloadAsync(
        Guid patientId,
        string epaDocumentId,
        string actorSub,
        string purpose,
        CancellationToken cancellationToken);
}

public sealed record EpaDownloadResult(
    bool Succeeded,
    string? MimeType,
    ReadOnlyMemory<byte> Content,
    string? FailureReason);
