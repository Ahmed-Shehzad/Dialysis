using Dialysis.Persistence.Abstractions;
using Dialysis.Persistence.Entities;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Persistence;

public sealed class ProcessedHl7MessageStore : IProcessedHl7MessageStore
{
    private readonly DialysisDbContext _db;

    public ProcessedHl7MessageStore(DialysisDbContext db)
    {
        _db = db;
    }

    public async Task<bool> ExistsAsync(TenantId tenantId, string messageControlId, CancellationToken cancellationToken = default)
    {
        return await _db.ProcessedHl7Messages
            .AnyAsync(
                p => p.TenantId == tenantId && p.MessageControlId == messageControlId,
                cancellationToken);
    }

    public async Task AddAsync(ProcessedHl7Message message, CancellationToken cancellationToken = default)
    {
        _db.ProcessedHl7Messages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
