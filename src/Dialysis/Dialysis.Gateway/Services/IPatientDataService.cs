using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Gateway.Services;

/// <summary>
/// Aggregates patient data from multiple sources. Single responsibility: load all patient data for export/push.
/// </summary>
public interface IPatientDataService
{
    Task<PatientDataAggregate?> GetAsync(TenantId tenantId, PatientId patientId, CancellationToken cancellationToken = default);
}
