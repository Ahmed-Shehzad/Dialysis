namespace Dialysis.HIE.Documents.Domain;

/// <summary>
/// Aggregate root for the HIE Documents index — one FHIR DocumentReference per browsable
/// clinical document. Holds only metadata: the actual bytes are persisted in the shared
/// <c>IDocumentBlobStore</c>, resolved by <see cref="StorageRef"/>. Mutation is through
/// the aggregate methods, never property setters, so audit invariants stay intact.
/// </summary>
public sealed class DocumentReference
{
    private readonly List<DocumentReferenceSignature> _signatures = [];

    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public string? Category { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public string? LanguageCode { get; private set; }
    public string StorageRef { get; private set; } = string.Empty;
    public string ContentHash { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DocumentReferenceSource Source { get; private set; }
    public DocumentReferenceStatus Status { get; private set; }
    public bool HasAcroForms { get; private set; }
    public bool HasJavascript { get; private set; }

    public IReadOnlyList<DocumentReferenceSignature> Signatures => _signatures;

    private DocumentReference() { }

    public DocumentReference(
        Guid id,
        Guid patientId,
        string kind,
        string title,
        string mimeType,
        string storageRef,
        string contentHash,
        long size,
        DocumentReferenceSource source,
        DateTime createdAtUtc,
        string? createdBy = null,
        string? category = null,
        string? languageCode = null,
        bool hasAcroForms = false,
        bool hasJavascript = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);
        if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));

        Id = id == Guid.Empty ? Guid.CreateVersion7() : id;
        PatientId = patientId;
        Kind = kind;
        Category = category;
        Title = title;
        MimeType = mimeType;
        StorageRef = storageRef;
        ContentHash = contentHash;
        Size = size;
        Source = source;
        Status = DocumentReferenceStatus.Current;
        CreatedAtUtc = createdAtUtc;
        CreatedBy = createdBy;
        LanguageCode = languageCode;
        HasAcroForms = hasAcroForms;
        HasJavascript = hasJavascript;
    }

    /// <summary>Records a new bytes version and updates the storage ref + hash.</summary>
    public void Revise(string storageRef, string contentHash, long size, bool hasAcroForms, bool hasJavascript)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);
        if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));
        if (Status != DocumentReferenceStatus.Current)
            throw new InvalidOperationException($"Cannot revise a {Status} document.");

        StorageRef = storageRef;
        ContentHash = contentHash;
        Size = size;
        HasAcroForms = hasAcroForms;
        HasJavascript = hasJavascript;
    }

    /// <summary>Soft-delete — sets status to <see cref="DocumentReferenceStatus.EnteredInError"/>.</summary>
    public void EnterInError()
    {
        if (Status == DocumentReferenceStatus.EnteredInError) return;
        Status = DocumentReferenceStatus.EnteredInError;
    }

    /// <summary>
    /// Combined retention / DSR-Art.-17 purge: marks the row entered-in-error AND replaces
    /// the storage ref with a tombstone (<c>purged://&lt;reason&gt;</c>). The caller is responsible
    /// for the physical blob delete via <c>IDocumentBlobStore.DeleteAsync</c>; this method
    /// captures the audit trail so a regulator can verify the row was deliberately purged
    /// rather than silently lost.
    /// </summary>
    public void MarkBlobPurged(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Status = DocumentReferenceStatus.EnteredInError;
        StorageRef = "purged://" + reason;
        Size = 0;
    }

    public void RecordSignature(DocumentReferenceSignature signature)
    {
        ArgumentNullException.ThrowIfNull(signature);
        if (Status != DocumentReferenceStatus.Current)
            throw new InvalidOperationException("Cannot sign a non-current document.");
        _signatures.Add(signature);
    }
}
