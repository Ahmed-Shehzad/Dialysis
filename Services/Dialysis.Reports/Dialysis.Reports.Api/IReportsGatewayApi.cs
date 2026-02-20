using Refit;

namespace Dialysis.Reports.Api;

public interface IReportsGatewayApi
{
    [Get("/api/treatment-sessions/reports/summary")]
    Task<SessionsSummaryReport> GetSessionsSummaryAsync(
        [Query] string from,
        [Query] string to,
        [Header("Authorization")] string? authorization,
        [Header("X-Tenant-Id")] string? tenantId,
        CancellationToken cancellationToken = default);

    [Get("/api/alarms")]
    Task<IApiResponse<AlarmsListResponse>> GetAlarmsAsync(
        [Query] string from,
        [Query] string to,
        [Header("Authorization")] string? authorization,
        [Header("X-Tenant-Id")] string? tenantId,
        CancellationToken cancellationToken = default);

    [Get("/api/treatment-sessions/fhir")]
    [Headers("Accept: application/fhir+json")]
    Task<IApiResponse<string>> GetTreatmentSessionsFhirAsync(
        [Query] string dateFrom,
        [Query] string dateTo,
        [Query] int limit,
        [Header("Authorization")] string? authorization,
        [Header("X-Tenant-Id")] string? tenantId,
        CancellationToken cancellationToken = default);

    [Get("/api/cds/prescription-compliance")]
    [Headers("Accept: application/fhir+json")]
    Task<IApiResponse<string>> GetPrescriptionComplianceAsync(
        [Query] string sessionId,
        [Header("Authorization")] string? authorization,
        [Header("X-Tenant-Id")] string? tenantId,
        CancellationToken cancellationToken = default);
}
