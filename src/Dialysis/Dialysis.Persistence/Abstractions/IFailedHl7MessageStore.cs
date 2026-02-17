using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Persistence.Abstractions;

/// <summary>
/// Dead-letter store for failed HL7 messages. Phase 4.1.3.
/// </summary>
public interface IFailedHl7MessageStore
{
    Task AddAsync(Entities.FailedHl7Message message, CancellationToken cancellationToken = default);
    Task<Entities.FailedHl7Message?> GetByIdAsync(TenantId tenantId, Ulid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.FailedHl7Message>> ListAsync(TenantId tenantId, int limit = 50, int offset = 0, CancellationToken cancellationToken = default);
    Task DeleteAsync(Entities.FailedHl7Message message, CancellationToken cancellationToken = default);
    Task IncrementRetryCountAsync(Entities.FailedHl7Message message, CancellationToken cancellationToken = default);
}
