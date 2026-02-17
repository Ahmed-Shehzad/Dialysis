using Dialysis.Domain.Entities;
using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Persistence.Abstractions;

public interface IAlertRepository
{
    Task AddAsync(Alert alert, CancellationToken cancellationToken = default);
    Task<Alert?> GetByIdAsync(TenantId tenantId, Ulid alertId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Alert>> GetByPatientAsync(TenantId tenantId, PatientId patientId, bool? activeOnly = null, int? limit = null, int offset = 0, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
