using Dialysis.HIE.Documents.Domain;
using Dialysis.HIE.Documents.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIE.Persistence.Repositories;

public sealed class EfDocumentRetentionPolicyRepository : IDocumentRetentionPolicyRepository
{
    private readonly HieDbContext _db;
    public EfDocumentRetentionPolicyRepository(HieDbContext db) => _db = db;
    public void Add(DocumentRetentionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        _db.DocumentRetentionPolicies.Add(policy);
    }

    public void Remove(DocumentRetentionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        _db.DocumentRetentionPolicies.Remove(policy);
    }

    public Task<DocumentRetentionPolicy?> FindByKindAsync(string kind, CancellationToken cancellationToken) =>
        _db.DocumentRetentionPolicies.FirstOrDefaultAsync(p => p.Kind == kind, cancellationToken);

    public async Task<IReadOnlyList<DocumentRetentionPolicy>> ListAsync(CancellationToken cancellationToken) =>
        await _db.DocumentRetentionPolicies
            .AsNoTracking()
            .OrderBy(p => p.Kind)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
