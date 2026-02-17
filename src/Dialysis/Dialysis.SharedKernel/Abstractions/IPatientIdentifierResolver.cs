using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.SharedKernel.Abstractions;

/// <summary>
/// Resolves patient identifiers (e.g. MRN) to internal PatientId. Phase 4.2.1 MPI.
/// </summary>
public interface IPatientIdentifierResolver
{
    /// <summary>
    /// Resolve an external identifier (e.g. MRN) to the PDMS PatientId for the tenant.
    /// Returns null if no matching patient is found.
    /// </summary>
    Task<PatientId?> ResolveByMrnAsync(TenantId tenantId, string mrn, CancellationToken cancellationToken = default);
}
