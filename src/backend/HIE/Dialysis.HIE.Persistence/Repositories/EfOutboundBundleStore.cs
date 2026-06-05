using Dialysis.HIE.Outbound.Domain;
using Dialysis.HIE.Outbound.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIE.Persistence.Repositories;

public sealed class EfOutboundBundleStore : IOutboundBundleStore
{
    private readonly HieDbContext _db;
    public EfOutboundBundleStore(HieDbContext db) => _db = db;
    public Task AddAsync(OutboundBundle bundle, CancellationToken cancellationToken = default) =>
        _db.OutboundBundles.AddAsync(bundle, cancellationToken).AsTask();

    public async Task<IReadOnlyList<OutboundBundle>> ClaimPendingAsync(int batchSize, DateTime asOfUtc, CancellationToken cancellationToken = default) =>
        await _db.OutboundBundles
            .Where(b => b.Status == OutboundBundleStatus.Pending && b.NextAttemptAtUtc <= asOfUtc)
            .OrderBy(b => b.NextAttemptAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<OutboundBundle>> ListAsync(OutboundBundleStatus? statusFilter, int take, CancellationToken cancellationToken = default)
    {
        var query = _db.OutboundBundles.AsNoTracking().AsQueryable();
        if (statusFilter.HasValue)
            query = query.Where(b => b.Status == statusFilter.Value);
        return await query
            .OrderByDescending(b => b.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<OutboundBundle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.OutboundBundles.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<OutboundBundle>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await _db.OutboundBundles.AsNoTracking()
            .Where(b => b.PatientId == patientId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
