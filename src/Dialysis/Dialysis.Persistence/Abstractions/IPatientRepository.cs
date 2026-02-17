using Dialysis.Domain.Entities;
using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Persistence.Abstractions;

/// <summary>
/// Repository for persisting and retrieving patients.
/// </summary>
public interface IPatientRepository
{
    Task AddAsync(Patient patient, CancellationToken cancellationToken = default);
    Task<Patient?> GetByIdAsync(TenantId tenantId, PatientId logicalId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TenantId tenantId, PatientId logicalId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default);
    Task DeleteAsync(Patient patient, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Patient>> ListAsync(TenantId tenantId, string? family = null, string? given = null, int? limit = null, int offset = 0, CancellationToken cancellationToken = default);
}
