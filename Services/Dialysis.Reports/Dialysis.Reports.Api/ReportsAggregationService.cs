using System.Text.Json;

using Refit;

namespace Dialysis.Reports.Api;

/// <summary>
/// Aggregates report data from Treatment and Alarm APIs.
/// </summary>
public sealed class ReportsAggregationService
{
    private readonly IReportsGatewayApi _api;
    private readonly ILogger<ReportsAggregationService> _logger;

    public ReportsAggregationService(IReportsGatewayApi api, ILogger<ReportsAggregationService> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<SessionsSummaryReport> GetSessionsSummaryAsync(DateTimeOffset from, DateTimeOffset to, string? tenantId, HttpRequest? request, CancellationToken cancellationToken = default)
    {
        string? auth = request?.Headers.Authorization.Count > 0 ? request.Headers.Authorization.ToString() : null;
        return await _api.GetSessionsSummaryAsync(from.ToString("o"), to.ToString("o"), auth, tenantId, cancellationToken);
    }

    public async Task<AlarmsBySeverityReport> GetAlarmsBySeverityAsync(DateTimeOffset from, DateTimeOffset to, string? tenantId, HttpRequest? request, CancellationToken cancellationToken = default)
    {
        string? auth = request?.Headers.Authorization.Count > 0 ? request.Headers.Authorization.ToString() : null;
        try
        {
            IApiResponse<AlarmsListResponse> response = await _api.GetAlarmsAsync(from.ToString("o"), to.ToString("o"), auth, tenantId, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Alarm API returned {StatusCode} for /api/alarms", (int)response.StatusCode);
                return new AlarmsBySeverityReport(new Dictionary<string, int>(), from, to);
            }

            AlarmsListResponse? listResponse = response.Content;
            IReadOnlyList<AlarmSummaryDto> alarms = listResponse?.Alarms ?? [];
            var bySeverity = alarms
                .GroupBy(a => a.Priority ?? "unknown")
                .ToDictionary(g => g.Key, g => g.Count());
            return new AlarmsBySeverityReport(bySeverity, from, to);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Alarm API request failed");
            return new AlarmsBySeverityReport(new Dictionary<string, int>(), from, to);
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "Alarm API request failed");
            return new AlarmsBySeverityReport(new Dictionary<string, int>(), from, to);
        }
    }

    public async Task<PrescriptionComplianceReport> GetPrescriptionComplianceAsync(DateTimeOffset from, DateTimeOffset to, string? tenantId, HttpRequest? request, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> sessionIds = await GetSessionIdsInRangeAsync(from, to, tenantId, request, cancellationToken);
        if (sessionIds.Count == 0)
            return new PrescriptionComplianceReport(0, 0, 0, from, to);

        int compliant = 0;
        const int maxSessions = 100;
        int toCheck = Math.Min(sessionIds.Count, maxSessions);
        string? auth = request?.Headers.Authorization.Count > 0 ? request.Headers.Authorization.ToString() : null;

        for (int i = 0; i < toCheck; i++)
        {
            IApiResponse<string> response = await _api.GetPrescriptionComplianceAsync(sessionIds[i], auth, tenantId, cancellationToken);
            if (!response.IsSuccessStatusCode)
                continue;
            string? json = response.Content;
            if (string.IsNullOrEmpty(json))
                continue;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("entry", out JsonElement entries))
                {
                    if (entries.GetArrayLength() == 0)
                        compliant++;
                }
                else
                    compliant++;
            }
            catch
            {
                // ignore parse errors
            }
        }

        decimal percent = toCheck > 0 ? Math.Round(100m * compliant / toCheck, 1) : 0;
        return new PrescriptionComplianceReport(compliant, toCheck, percent, from, to);
    }

    private async Task<IReadOnlyList<string>> GetSessionIdsInRangeAsync(DateTimeOffset from, DateTimeOffset to, string? tenantId, HttpRequest? request, CancellationToken ct)
    {
        string? auth = request?.Headers.Authorization.Count > 0 ? request.Headers.Authorization.ToString() : null;
        IApiResponse<string> response = await _api.GetTreatmentSessionsFhirAsync(from.ToString("o"), to.ToString("o"), 100, auth, tenantId, ct);
        if (!response.IsSuccessStatusCode || response.Content is null)
            return [];
        return FhirBundleParser.ExtractProcedureSessionIds(response.Content);
    }

    public async Task<TreatmentDurationByPatientReport> GetTreatmentDurationByPatientAsync(DateTimeOffset from, DateTimeOffset to, string? tenantId, HttpRequest? request, CancellationToken cancellationToken = default)
    {
        string? auth = request?.Headers.Authorization.Count > 0 ? request.Headers.Authorization.ToString() : null;
        IApiResponse<string> response = await _api.GetTreatmentSessionsFhirAsync(from.ToString("o"), to.ToString("o"), 1000, auth, tenantId, cancellationToken);
        if (!response.IsSuccessStatusCode || response.Content is null)
            return new TreatmentDurationByPatientReport([], from, to);
        IReadOnlyList<PatientDurationSummary> byPatient = FhirBundleParser.ParseDurationByPatient(response.Content);
        return new TreatmentDurationByPatientReport(byPatient, from, to);
    }

    public async Task<ObservationsSummaryReport> GetObservationsSummaryAsync(DateTimeOffset from, DateTimeOffset to, string? codeFilter, string? tenantId, HttpRequest? request, CancellationToken cancellationToken = default)
    {
        string? auth = request?.Headers.Authorization.Count > 0 ? request.Headers.Authorization.ToString() : null;
        IApiResponse<string> response = await _api.GetTreatmentSessionsFhirAsync(from.ToString("o"), to.ToString("o"), 1000, auth, tenantId, cancellationToken);
        if (!response.IsSuccessStatusCode || response.Content is null)
            return new ObservationsSummaryReport([], from, to);
        IReadOnlyList<ObservationCountByCode> byCode = FhirBundleParser.ParseObservationsByCode(response.Content, codeFilter);
        return new ObservationsSummaryReport(byCode, from, to);
    }
}

public sealed record SessionsSummaryReport(int SessionCount, decimal AvgDurationMinutes, DateTimeOffset From, DateTimeOffset To);
public sealed record AlarmsBySeverityReport(IReadOnlyDictionary<string, int> BySeverity, DateTimeOffset From, DateTimeOffset To);
public sealed record PrescriptionComplianceReport(int CompliantCount, int TotalEvaluated, decimal CompliancePercent, DateTimeOffset From, DateTimeOffset To);
public sealed record TreatmentDurationByPatientReport(IReadOnlyList<PatientDurationSummary> ByPatient, DateTimeOffset From, DateTimeOffset To);
public sealed record PatientDurationSummary(string PatientMrn, int SessionCount, decimal TotalMinutes, decimal AvgMinutesPerSession);
public sealed record ObservationsSummaryReport(IReadOnlyList<ObservationCountByCode> ByCode, DateTimeOffset From, DateTimeOffset To);
public sealed record ObservationCountByCode(string Code, int Count);

public sealed record AlarmsListResponse(IReadOnlyList<AlarmSummaryDto> Alarms);
public sealed record AlarmSummaryDto(string? Priority);
