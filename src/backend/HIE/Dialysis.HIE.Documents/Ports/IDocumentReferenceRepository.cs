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
