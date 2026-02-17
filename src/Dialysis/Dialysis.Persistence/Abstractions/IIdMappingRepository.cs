using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Persistence.Abstractions;

/// <summary>
/// Cross-system ID mapping. Phase 4.2.2.
/// </summary>
public interface IIdMappingRepository
{
    Task AddAsync(Entities.IdMapping mapping, CancellationToken cancellationToken = default);
    Task<Entities.IdMapping?> GetByLocalAsync(TenantId tenantId, string resourceType, string localId, string externalSystem, CancellationToken cancellationToken = default);
    Task<Entities.IdMapping?> GetByExternalAsync(TenantId tenantId, string resourceType, string externalSystem, string externalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.IdMapping>> ListByResourceAsync(TenantId tenantId, string resourceType, int limit = 50, int offset = 0, CancellationToken cancellationToken = default);
}
