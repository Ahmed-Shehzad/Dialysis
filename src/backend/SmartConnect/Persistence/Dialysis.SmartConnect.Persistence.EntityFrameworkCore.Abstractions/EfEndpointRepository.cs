using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

public sealed class EfEndpointRepository(SmartConnectDbContext db, TimeProvider time) : IEndpointRepository
{
    public Task<EndpointEntity?> GetByNameAsync(string name, CancellationToken cancellationToken) =>
        db.Endpoints.AsNoTracking().FirstOrDefaultAsync(e => e.Name == name, cancellationToken);

    public Task<EndpointEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Endpoints.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<EndpointEntity>> ListAsync(CancellationToken cancellationToken) =>
        await db.Endpoints.AsNoTracking().OrderBy(e => e.Name).ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task AddAsync(EndpointEntity entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (entity.Id == Guid.Empty) entity.Id = Guid.CreateVersion7();
        var now = time.GetUtcNow();
        entity.CreatedAtUtc = now;
        entity.UpdatedAtUtc = now;
        await db.Endpoints.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(EndpointEntity entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);
        entity.UpdatedAtUtc = time.GetUtcNow();
        db.Endpoints.Update(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var existing = await db.Endpoints.FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);
        if (existing is null) return false;
        db.Endpoints.Remove(existing);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
