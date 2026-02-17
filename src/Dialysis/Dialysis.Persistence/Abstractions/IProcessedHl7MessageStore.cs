using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Persistence.Abstractions;

/// <summary>
/// Idempotency store for HL7 messages (MSH-10). Phase 4.1.3.
/// </summary>
public interface IProcessedHl7MessageStore
{
    Task<bool> ExistsAsync(TenantId tenantId, string messageControlId, CancellationToken cancellationToken = default);
    Task AddAsync(Entities.ProcessedHl7Message message, CancellationToken cancellationToken = default);
}
