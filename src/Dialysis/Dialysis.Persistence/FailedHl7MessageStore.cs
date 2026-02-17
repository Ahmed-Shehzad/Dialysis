using Dialysis.Persistence.Abstractions;
using Dialysis.Persistence.Entities;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Persistence;

public sealed class FailedHl7MessageStore : IFailedHl7MessageStore
{
    private readonly DialysisDbContext _db;

    public FailedHl7MessageStore(DialysisDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(FailedHl7Message message, CancellationToken cancellationToken = default)
    {
        _db.FailedHl7Messages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<FailedHl7Message?> GetByIdAsync(TenantId tenantId, Ulid id, CancellationToken cancellationToken = default)
    {
        return await _db.FailedHl7Messages
            .FirstOrDefaultAsync(
                f => f.TenantId == tenantId && f.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<FailedHl7Message>> ListAsync(TenantId tenantId, int limit = 50, int offset = 0, CancellationToken cancellationToken = default)
    {
        return await _db.FailedHl7Messages
            .Where(f => f.TenantId == tenantId)
            .OrderByDescending(f => f.FailedAtUtc)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(FailedHl7Message message, CancellationToken cancellationToken = default)
    {
        _db.FailedHl7Messages.Remove(message);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task IncrementRetryCountAsync(FailedHl7Message message, CancellationToken cancellationToken = default)
    {
        message.RetryCount++;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
