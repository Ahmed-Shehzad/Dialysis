using Dialysis.Domain.Aggregates;
using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Persistence.Abstractions;

/// <summary>
/// Repository for medication administrations (ESA, iron, heparin, binders).
/// </summary>
public interface IMedicationAdministrationRepository
{
    Task AddAsync(MedicationAdministration medication, CancellationToken cancellationToken = default);
    Task<MedicationAdministration?> GetAsync(TenantId tenantId, string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicationAdministration>> ListByPatientAsync(
        TenantId tenantId,
        PatientId patientId,
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicationAdministration>> ListBySessionAsync(
        TenantId tenantId,
        string sessionId,
        CancellationToken cancellationToken = default);
}
