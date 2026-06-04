using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

public sealed class EfEndpointRepository : IEndpointRepository
{
    private readonly SmartConnectDbContext _db;
    private readonly TimeProvider _time;
    public EfEndpointRepository(SmartConnectDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }
    public Task<EndpointEntity?> GetByNameAsync(string name, CancellationToken cancellationToken) =>
        _db.Endpoints.AsNoTracking().FirstOrDefaultAsync(e => e.Name == name, cancellationToken);

    public Task<EndpointEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Endpoints.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<EndpointEntity>> ListAsync(CancellationToken cancellationToken) =>
        await _db.Endpoints.AsNoTracking().OrderBy(e => e.Name).ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task AddAsync(EndpointEntity entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (entity.Id == Guid.Empty) entity.Id = Guid.CreateVersion7();
        var now = _time.GetUtcNow();
        entity.CreatedAtUtc = now;
        entity.UpdatedAtUtc = now;
        await _db.Endpoints.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(EndpointEntity entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);
        entity.UpdatedAtUtc = _time.GetUtcNow();
        _db.Endpoints.Update(entity);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var existing = await _db.Endpoints.FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);
        if (existing is null) return false;
        _db.Endpoints.Remove(existing);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
