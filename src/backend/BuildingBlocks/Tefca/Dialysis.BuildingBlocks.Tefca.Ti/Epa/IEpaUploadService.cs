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

/// 
public sealed record EpaUploadRequest
{
    /// <param name="Language">
    /// BCP-47 language tag of the document content (e.g. <c>de</c>, <c>en-US</c>). Carried into the
    /// ePA metadata so the patient's record stores the document's language. Optional — defaults to
    /// <c>de</c> when the caller doesn't resolve a language.
    /// </param>
    public EpaUploadRequest(Guid PatientId,
        string DocumentTitle,
        string MimeType,
        ReadOnlyMemory<byte> Content,
        string ActorSub,
        string Purpose,
        string? Language = null)
    {
        this.PatientId = PatientId;
        this.DocumentTitle = DocumentTitle;
        this.MimeType = MimeType;
        this.Content = Content;
        this.ActorSub = ActorSub;
        this.Purpose = Purpose;
        this.Language = Language;
    }
    public Guid PatientId { get; init; }
    public string DocumentTitle { get; init; }
    public string MimeType { get; init; }
    public ReadOnlyMemory<byte> Content { get; init; }
    public string ActorSub { get; init; }
    public string Purpose { get; init; }

    /// <summary>
    /// BCP-47 language tag of the document content (e.g. <c>de</c>, <c>en-US</c>). Carried into the
    /// ePA metadata so the patient's record stores the document's language. Optional — defaults to
    /// <c>de</c> when the caller doesn't resolve a language.
    /// </summary>
    public string? Language { get; init; }

    public void Deconstruct(out Guid PatientId, out string DocumentTitle, out string MimeType, out ReadOnlyMemory<byte> Content, out string ActorSub, out string Purpose, out string? Language)
    {
        PatientId = this.PatientId;
        DocumentTitle = this.DocumentTitle;
        MimeType = this.MimeType;
        Content = this.Content;
        ActorSub = this.ActorSub;
        Purpose = this.Purpose;
        Language = this.Language;
    }
}

public sealed record EpaUploadResult
{
    public EpaUploadResult(bool Succeeded,
        string? EpaDocumentId,
        string? FailureReason)
    {
        this.Succeeded = Succeeded;
        this.EpaDocumentId = EpaDocumentId;
        this.FailureReason = FailureReason;
    }
    public bool Succeeded { get; init; }
    public string? EpaDocumentId { get; init; }
    public string? FailureReason { get; init; }
    public void Deconstruct(out bool Succeeded, out string? EpaDocumentId, out string? FailureReason)
    {
        Succeeded = this.Succeeded;
        EpaDocumentId = this.EpaDocumentId;
        FailureReason = this.FailureReason;
    }
}

public interface IEpaDownloadService
{
    Task<EpaDownloadResult> DownloadAsync(
        Guid patientId,
        string epaDocumentId,
        string actorSub,
        string purpose,
        CancellationToken cancellationToken);
}

public sealed record EpaDownloadResult
{
    public EpaDownloadResult(bool Succeeded,
        string? MimeType,
        ReadOnlyMemory<byte> Content,
        string? FailureReason)
    {
        this.Succeeded = Succeeded;
        this.MimeType = MimeType;
        this.Content = Content;
        this.FailureReason = FailureReason;
    }
    public bool Succeeded { get; init; }
    public string? MimeType { get; init; }
    public ReadOnlyMemory<byte> Content { get; init; }
    public string? FailureReason { get; init; }
    public void Deconstruct(out bool Succeeded, out string? MimeType, out ReadOnlyMemory<byte> Content, out string? FailureReason)
    {
        Succeeded = this.Succeeded;
        MimeType = this.MimeType;
        Content = this.Content;
        FailureReason = this.FailureReason;
    }
}
