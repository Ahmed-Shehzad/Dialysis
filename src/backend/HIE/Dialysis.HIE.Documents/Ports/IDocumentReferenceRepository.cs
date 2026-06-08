using Dialysis.HIE.Documents.Domain;

namespace Dialysis.HIE.Documents.Ports;

/// <summary>
/// Repository for the <see cref="DocumentReference"/> aggregate. Cross-module callers
/// (the OnClinicalDocumentProduced consumer, the admin controller) talk to this port; the
/// EF implementation lives in <c>Dialysis.HIE.Persistence</c>.
/// </summary>
public interface IDocumentReferenceRepository
{
    void Add(DocumentReference document);

    /// <summary>
    /// Inserts the document idempotently, returning <see langword="true"/> if it created the row and
    /// <see langword="false"/> if an equal-primary-key row already existed (idempotent redelivery,
    /// including the concurrent read-then-insert race). The document id is the source event id, so a
    /// primary-key conflict means the document was already indexed — never an error to surface.
    /// </summary>
    Task<bool> TryAddIdempotentAsync(DocumentReference document, CancellationToken cancellationToken);

    Task<DocumentReference?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Admin list — returns the most recent documents matching the supplied filters. Excludes
    /// <see cref="DocumentReferenceStatus.EnteredInError"/> unless the caller asks for it.
    /// </summary>
    Task<IReadOnlyList<DocumentReference>> ListAsync(
        Guid? patientId,
        string? kind,
        DocumentReferenceStatus? status,
        DocumentReferenceSource? source,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retention-purge read — returns every <see cref="DocumentReferenceStatus.Current"/>
    /// document of the given kind whose <c>CreatedAtUtc</c> is strictly before
    /// <paramref name="createdBefore"/>. Bounded by <paramref name="take"/> so the purger
    /// processes a manageable batch per tick.
    /// </summary>
    Task<IReadOnlyList<DocumentReference>> ListExpiredAsync(
        string kind,
        DateTime createdBefore,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// DSR Art. 17 read — returns every <see cref="DocumentReferenceStatus.Current"/>
    /// document owned by the patient regardless of kind / source.
    /// </summary>
    Task<IReadOnlyList<DocumentReference>> ListForPatientAsync(
        Guid patientId, CancellationToken cancellationToken);
}
