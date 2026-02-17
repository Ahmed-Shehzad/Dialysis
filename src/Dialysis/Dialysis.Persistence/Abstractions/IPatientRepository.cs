using Dialysis.Domain.Entities;

namespace Dialysis.Persistence.Abstractions;

/// <summary>
/// Write-only repository for patients.
/// </summary>
public interface IPatientRepository
{
    Task AddAsync(Patient patient, CancellationToken cancellationToken = default);
    Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default);
    Task DeleteAsync(Patient patient, CancellationToken cancellationToken = default);
}
