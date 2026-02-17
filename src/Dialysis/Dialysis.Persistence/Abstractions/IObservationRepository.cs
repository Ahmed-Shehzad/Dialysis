using Dialysis.Domain.Aggregates;
using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Persistence.Abstractions;

/// <summary>
/// Repository for persisting and querying observations.
/// </summary>
public interface IObservationRepository
{
    Task AddAsync(Observation observation, CancellationToken cancellationToken = default);
    Task<Observation?> GetByIdAsync(TenantId tenantId, ObservationId observationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Observation>> GetByPatientAsync(TenantId tenantId, PatientId patientId, CancellationToken cancellationToken = default);
}
