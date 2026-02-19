namespace Dialysis.Prescription.Application.Abstractions;

/// <summary>
/// Read-only store for Prescription queries. Used by query handlers instead of the write repository.
/// </summary>
public interface IPrescriptionReadStore
{
    Task<PrescriptionReadDto?> GetLatestByMrnAsync(string tenantId, string mrn, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PrescriptionReadDto>> GetAllForTenantAsync(string tenantId, int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PrescriptionReadDto>> GetByPatientMrnAsync(string tenantId, string mrn, int limit, CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for prescription query results. Includes resolved settings (blood flow, UF rate, UF target).
/// </summary>
public sealed record PrescriptionReadDto(
    string OrderId,
    string PatientMrn,
    string? Modality,
    string? OrderingProvider,
    decimal? BloodFlowRateMlMin,
    decimal? UfRateMlH,
    decimal? UfTargetVolumeMl,
    DateTimeOffset? ReceivedAt);
