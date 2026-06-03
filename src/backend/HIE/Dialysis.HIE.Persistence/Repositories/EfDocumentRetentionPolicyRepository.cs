using Dialysis.HIE.Documents.Domain;
using Dialysis.HIE.Documents.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIE.Persistence.Repositories;

public sealed class EfDocumentRetentionPolicyRepository(HieDbContext db) : IDocumentRetentionPolicyRepository
{
    public void Add(DocumentRetentionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        db.DocumentRetentionPolicies.Add(policy);
    }

    public void Remove(DocumentRetentionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        db.DocumentRetentionPolicies.Remove(policy);
    }

    public Task<DocumentRetentionPolicy?> FindByKindAsync(string kind, CancellationToken cancellationToken) =>
        db.DocumentRetentionPolicies.FirstOrDefaultAsync(p => p.Kind == kind, cancellationToken);

    public async Task<IReadOnlyList<DocumentRetentionPolicy>> ListAsync(CancellationToken cancellationToken) =>
        await db.DocumentRetentionPolicies
            .AsNoTracking()
            .OrderBy(p => p.Kind)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
