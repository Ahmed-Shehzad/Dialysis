using Dialysis.HIS.DataServices.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfIntegrationOutboxMetadataReadModel(HisDbContext db) : IIntegrationOutboxMetadataReadModel
{
    public async Task<IReadOnlyList<IntegrationOutboxMetadataRow>> ListRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);
        return await db.OutboxMessages.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .Select(x => new IntegrationOutboxMetadataRow(
                x.Id,
                x.AssemblyQualifiedEventType,
                x.CreatedAtUtc,
                x.ProcessedAtUtc,
                x.CorrelationId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
