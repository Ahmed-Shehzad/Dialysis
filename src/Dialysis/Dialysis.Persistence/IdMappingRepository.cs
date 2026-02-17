using Dialysis.Persistence.Abstractions;
using Dialysis.Persistence.Entities;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Persistence;

public sealed class IdMappingRepository : IIdMappingRepository
{
    private readonly DialysisDbContext _db;

    public IdMappingRepository(DialysisDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(IdMapping mapping, CancellationToken cancellationToken = default)
    {
        _db.IdMappings.Add(mapping);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IdMapping?> GetByLocalAsync(TenantId tenantId, string resourceType, string localId, string externalSystem, CancellationToken cancellationToken = default)
    {
        return await _db.IdMappings
            .FirstOrDefaultAsync(
                m => m.TenantId == tenantId.Value && m.ResourceType == resourceType && m.LocalId == localId && m.ExternalSystem == externalSystem,
                cancellationToken);
    }

    public async Task<IdMapping?> GetByExternalAsync(TenantId tenantId, string resourceType, string externalSystem, string externalId, CancellationToken cancellationToken = default)
    {
        return await _db.IdMappings
            .FirstOrDefaultAsync(
                m => m.TenantId == tenantId.Value && m.ResourceType == resourceType && m.ExternalSystem == externalSystem && m.ExternalId == externalId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<IdMapping>> ListByResourceAsync(TenantId tenantId, string resourceType, int limit = 50, int offset = 0, CancellationToken cancellationToken = default)
    {
        return await _db.IdMappings
            .Where(m => m.TenantId == tenantId.Value && m.ResourceType == resourceType)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
