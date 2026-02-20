using Dialysis.Cds.Api.Controllers;

using Refit;

namespace Dialysis.Cds.Api;

/// <summary>
/// Refit client for CDS gateway - fetches treatment sessions and prescriptions from Treatment/Prescription APIs.
/// </summary>
public interface ICdsGatewayApi
{
    [Get("/api/treatment-sessions/{sessionId}")]
    Task<TreatmentSessionResponse> GetTreatmentSessionAsync(
        string sessionId,
        [Header("Authorization")] string? authorization,
        [Header("X-Tenant-Id")] string? tenantId,
        CancellationToken cancellationToken = default);

    [Get("/api/prescriptions/{mrn}")]
    Task<IApiResponse<PrescriptionByMrnResponse>> GetPrescriptionByMrnAsync(
        string mrn,
        [Header("Authorization")] string? authorization,
        [Header("X-Tenant-Id")] string? tenantId,
        CancellationToken cancellationToken = default);
}
