using Dialysis.HIE.Documents.Domain;

namespace Dialysis.HIE.Documents.Ports;

/// <summary>
/// Repository for the <see cref="DocumentRetentionPolicy"/> aggregate. The admin controller
/// drives upsert/delete; the purger job iterates <see cref="ListAsync"/>.
/// </summary>
public interface IDocumentRetentionPolicyRepository
{
    void Add(DocumentRetentionPolicy policy);

    void Remove(DocumentRetentionPolicy policy);

    Task<DocumentRetentionPolicy?> FindByKindAsync(string kind, CancellationToken cancellationToken);

    Task<IReadOnlyList<DocumentRetentionPolicy>> ListAsync(CancellationToken cancellationToken);
}
