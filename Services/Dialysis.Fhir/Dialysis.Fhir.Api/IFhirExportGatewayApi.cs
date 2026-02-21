using Refit;

namespace Dialysis.Fhir.Api;

/// <summary>
/// Refit client for FHIR bulk export - fetches bundles from Patient, Device, Prescription, Treatment, and Alarm services.
/// </summary>
public interface IFhirExportGatewayApi
{
    [Get("/api/patients/fhir")]
    [Headers("Accept: application/fhir+json")]
    Task<HttpResponseMessage> GetPatientsFhirAsync(
        [Query] int limit,
        [Query("_id")] string? id,
        [Query] string? identifier,
        [Header("Authorization")] string? authorization,
        [Header("X-Tenant-Id")] string? tenantId,
        CancellationToken cancellationToken = default);

    [Get("/api/devices/fhir")]
    [Headers("Accept: application/fhir+json")]
    Task<HttpResponseMessage> GetDevicesFhirAsync(
        [Query("_id")] string? id,
        [Query] string? identifier,
        [Header("Authorization")] string? authorization,
        [Header("X-Tenant-Id")] string? tenantId,
        CancellationToken cancellationToken = default);

    [Get("/api/prescriptions/fhir")]
    [Headers("Accept: application/fhir+json")]
    Task<HttpResponseMessage> GetPrescriptionsFhirAsync(
        [Query] int limit,
        [Query] string? subject,
        [Query] string? patient,
        [Header("Authorization")] string? authorization,
        [Header("X-Tenant-Id")] string? tenantId,
        CancellationToken cancellationToken = default);

    [Get("/api/treatment-sessions/fhir")]
    [Headers("Accept: application/fhir+json")]
    Task<HttpResponseMessage> GetTreatmentSessionsFhirAsync(
        [Query] int limit,
        [Query] string? subject,
        [Query] string? patient,
        [Query] string? date,
        [Query] string? dateFrom,
        [Query] string? dateTo,
        [Header("Authorization")] string? authorization,
        [Header("X-Tenant-Id")] string? tenantId,
        CancellationToken cancellationToken = default);

    [Get("/api/alarms/fhir")]
    [Headers("Accept: application/fhir+json")]
    Task<HttpResponseMessage> GetAlarmsFhirAsync(
        [Query] int limit,
        [Query("_id")] string? id,
        [Query] string? deviceId,
        [Query] string? sessionId,
        [Query] string? date,
        [Query] string? from,
        [Query] string? to,
        [Header("Authorization")] string? authorization,
        [Header("X-Tenant-Id")] string? tenantId,
        CancellationToken cancellationToken = default);

    [Get("/api/audit-events")]
    [Headers("Accept: application/fhir+json")]
    Task<HttpResponseMessage> GetAuditEventsAsync(
        [Query] int count,
        [Header("Authorization")] string? authorization,
        [Header("X-Tenant-Id")] string? tenantId,
        CancellationToken cancellationToken = default);
}
