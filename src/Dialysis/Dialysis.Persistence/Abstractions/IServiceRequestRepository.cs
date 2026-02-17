using Dialysis.Domain.Entities;
using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Persistence.Abstractions;

public interface IServiceRequestRepository
{
    Task AddAsync(ServiceRequest order, CancellationToken cancellationToken = default);
    Task<ServiceRequest?> GetAsync(TenantId tenantId, string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceRequest>> ListByPatientAsync(
        TenantId tenantId,
        PatientId patientId,
        string? status = null,
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default);
}
