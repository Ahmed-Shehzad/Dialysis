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
}
