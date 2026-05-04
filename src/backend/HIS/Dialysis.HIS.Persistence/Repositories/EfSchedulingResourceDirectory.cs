using Dialysis.HIS.Scheduling.Domain;
using Dialysis.HIS.Scheduling.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfSchedulingResourceDirectory(HisDbContext db) : ISchedulingResourceDirectory
{
    public Task<SchedulingResource?> GetAsync(Guid resourceId, CancellationToken cancellationToken = default) =>
        db.SchedulingResources.AsNoTracking().FirstOrDefaultAsync(r => r.Id == resourceId, cancellationToken);

    public async Task<IReadOnlyList<SchedulingResource>> ListAsync(string? kindCode, CancellationToken cancellationToken = default)
    {
        var q = db.SchedulingResources.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(kindCode))
            q = q.Where(r => r.KindCode == kindCode);
        return await q.OrderBy(r => r.KindCode).ThenBy(r => r.DisplayName).ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
