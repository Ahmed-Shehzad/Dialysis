using Dialysis.Domain.Entities;
using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Persistence.Abstractions;

public interface IVascularAccessRepository
{
    Task AddAsync(VascularAccess access, CancellationToken cancellationToken = default);
    Task<VascularAccess?> GetByIdAsync(TenantId tenantId, Ulid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VascularAccess>> GetByPatientAsync(TenantId tenantId, PatientId patientId, VascularAccessStatus? status = null, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
