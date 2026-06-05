using Dialysis.HIE.Inbound.Terminology;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIE.Persistence.Repositories;

/// <summary>EF-backed <see cref="IAuthoredTerminologyRepository"/> over <see cref="HieDbContext"/>.</summary>
public sealed class EfAuthoredTerminologyRepository : IAuthoredTerminologyRepository
{
    private readonly HieDbContext _db;
    public EfAuthoredTerminologyRepository(HieDbContext db) => _db = db;

    public void Add(AuthoredTerminologyResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        _db.AuthoredTerminologyResources.Add(resource);
    }

    public void Remove(AuthoredTerminologyResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        _db.AuthoredTerminologyResources.Remove(resource);
    }

    public Task<AuthoredTerminologyResource?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        _db.AuthoredTerminologyResources.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<AuthoredTerminologyResource?> FindByUrlVersionAsync(string url, string version, CancellationToken cancellationToken) =>
        _db.AuthoredTerminologyResources.FirstOrDefaultAsync(r => r.Url == url && r.Version == version, cancellationToken);

    public async Task<IReadOnlyList<AuthoredTerminologyResource>> ListAsync(CancellationToken cancellationToken) =>
        await _db.AuthoredTerminologyResources
            .AsNoTracking()
            .OrderBy(r => r.Url).ThenBy(r => r.Version)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<AuthoredTerminologyResource>> ListActiveAsync(CancellationToken cancellationToken) =>
        await _db.AuthoredTerminologyResources
            .AsNoTracking()
            .Where(r => r.Status == "active")
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
