using Dialysis.Domain.Entities;
using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Persistence.Abstractions;

public interface IAuditRepository
{
    Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditEvent>> QueryAsync(
        TenantId tenantId,
        string? patientId = null,
        string? resourceType = null,
        string? action = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int limit = 100,
        int offset = 0,
        CancellationToken cancellationToken = default);
}
