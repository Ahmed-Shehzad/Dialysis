namespace Dialysis.Reports.Api;

/// <summary>
/// Aggregates report data from Treatment and Alarm APIs.
/// </summary>
public sealed class ReportsAggregationService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public ReportsAggregationService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<SessionsSummaryReport> GetSessionsSummaryAsync(DateTimeOffset from, DateTimeOffset to, string? tenantId, HttpRequest? request, CancellationToken cancellationToken = default)
    {
        string baseUrl = _config["Reports:BaseUrl"] ?? "http://localhost:5000";
        string url = baseUrl.TrimEnd('/') + $"/api/treatment-sessions/reports/summary?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        AddHeaders(req, request, tenantId);
        using (HttpResponseMessage response = await _http.SendAsync(req, cancellationToken))
        {
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SessionsSummaryReport>(cancellationToken) ?? new SessionsSummaryReport(0, 0, from, to);
        }
    }

    public async Task<AlarmsBySeverityReport> GetAlarmsBySeverityAsync(DateTimeOffset from, DateTimeOffset to, string? tenantId, HttpRequest? request, CancellationToken cancellationToken = default)
    {
        string baseUrl = _config["Reports:BaseUrl"] ?? "http://localhost:5000";
        string alarmsUrl = baseUrl.TrimEnd('/') + $"/api/alarms?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}";
        using var req = new HttpRequestMessage(HttpMethod.Get, alarmsUrl);
        AddHeaders(req, request, tenantId);
        using (HttpResponseMessage response = await _http.SendAsync(req, cancellationToken))
        {
            response.EnsureSuccessStatusCode();
            AlarmsListResponse? listResponse = await response.Content.ReadFromJsonAsync<AlarmsListResponse>(cancellationToken);
            IReadOnlyList<AlarmSummaryDto> alarms = listResponse?.Alarms ?? [];
            var bySeverity = alarms
                .GroupBy(a => a.Priority ?? "unknown")
                .ToDictionary(g => g.Key, g => g.Count());
            return new AlarmsBySeverityReport(bySeverity, from, to);
        }
    }

    /// <summary>
    /// Computes prescription compliance rate: % of sessions within ±10% of prescribed blood flow, UF rate, UF target.
    /// </summary>
    public async Task<PrescriptionComplianceReport> GetPrescriptionComplianceAsync(DateTimeOffset from, DateTimeOffset to, string? tenantId, HttpRequest? request, CancellationToken cancellationToken = default)
    {
        string baseUrl = _config["Reports:BaseUrl"] ?? "http://localhost:5000";
        IReadOnlyList<string> sessionIds = await GetSessionIdsInRangeAsync(baseUrl, from, to, tenantId, request, cancellationToken);
        if (sessionIds.Count == 0)
            return new PrescriptionComplianceReport(0, 0, 0, from, to);

        int compliant = 0;
        const int maxSessions = 100;
        int toCheck = Math.Min(sessionIds.Count, maxSessions);

        for (int i = 0; i < toCheck; i++)
            if (await IsSessionCompliantAsync(baseUrl, sessionIds[i], tenantId, request, cancellationToken))
                compliant++;

        decimal percent = toCheck > 0 ? Math.Round(100m * compliant / toCheck, 1) : 0;
        return new PrescriptionComplianceReport(compliant, toCheck, percent, from, to);
    }

    private async Task<IReadOnlyList<string>> GetSessionIdsInRangeAsync(string baseUrl, DateTimeOffset from, DateTimeOffset to, string? tenantId, HttpRequest? request, CancellationToken ct)
    {
        string url = baseUrl.TrimEnd('/') + $"/api/treatment-sessions/fhir?dateFrom={Uri.EscapeDataString(from.ToString("O"))}&dateTo={Uri.EscapeDataString(to.ToString("O"))}&limit=100";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        AddHeaders(req, request, tenantId);
        _ = req.Headers.TryAddWithoutValidation("Accept", "application/fhir+json");
        using (HttpResponseMessage response = await _http.SendAsync(req, ct))
        {
            if (!response.IsSuccessStatusCode)
                return [];
            string json = await response.Content.ReadAsStringAsync(ct);
            return ExtractSessionIdsFromFhirBundle(json);
        }
    }

    private static IReadOnlyList<string> ExtractSessionIdsFromFhirBundle(string json) =>
        FhirBundleParser.ExtractProcedureSessionIds(json);

    private async Task<bool> IsSessionCompliantAsync(string baseUrl, string sessionId, string? tenantId, HttpRequest? request, CancellationToken ct)
    {
        string url = baseUrl.TrimEnd('/') + $"/api/cds/prescription-compliance?sessionId={Uri.EscapeDataString(sessionId)}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        AddHeaders(req, request, tenantId);
        _ = req.Headers.TryAddWithoutValidation("Accept", "application/fhir+json");
        using (HttpResponseMessage response = await _http.SendAsync(req, ct))
        {
            if (!response.IsSuccessStatusCode)
                return false;
            string json = await response.Content.ReadAsStringAsync(ct);
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("entry", out var entries))
                    return entries.GetArrayLength() == 0;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public async Task<TreatmentDurationByPatientReport> GetTreatmentDurationByPatientAsync(DateTimeOffset from, DateTimeOffset to, string? tenantId, HttpRequest? request, CancellationToken cancellationToken = default)
    {
        string baseUrl = _config["Reports:BaseUrl"] ?? "http://localhost:5000";
        string url = baseUrl.TrimEnd('/') + $"/api/treatment-sessions/fhir?dateFrom={Uri.EscapeDataString(from.ToString("O"))}&dateTo={Uri.EscapeDataString(to.ToString("O"))}&limit=1000";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        AddHeaders(req, request, tenantId);
        _ = req.Headers.TryAddWithoutValidation("Accept", "application/fhir+json");
        using (HttpResponseMessage response = await _http.SendAsync(req, cancellationToken))
        {
            if (!response.IsSuccessStatusCode)
                return new TreatmentDurationByPatientReport([], from, to);
            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            var byPatient = FhirBundleParser.ParseDurationByPatient(json);
            return new TreatmentDurationByPatientReport(byPatient, from, to);
        }
    }

    public async Task<ObservationsSummaryReport> GetObservationsSummaryAsync(DateTimeOffset from, DateTimeOffset to, string? codeFilter, string? tenantId, HttpRequest? request, CancellationToken cancellationToken = default)
    {
        string baseUrl = _config["Reports:BaseUrl"] ?? "http://localhost:5000";
        string url = baseUrl.TrimEnd('/') + $"/api/treatment-sessions/fhir?dateFrom={Uri.EscapeDataString(from.ToString("O"))}&dateTo={Uri.EscapeDataString(to.ToString("O"))}&limit=1000";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        AddHeaders(req, request, tenantId);
        _ = req.Headers.TryAddWithoutValidation("Accept", "application/fhir+json");
        using (HttpResponseMessage response = await _http.SendAsync(req, cancellationToken))
        {
            if (!response.IsSuccessStatusCode)
                return new ObservationsSummaryReport([], from, to);
            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            var byCode = FhirBundleParser.ParseObservationsByCode(json, codeFilter);
            return new ObservationsSummaryReport(byCode, from, to);
        }
    }

    private static void AddHeaders(HttpRequestMessage message, HttpRequest? request, string? tenantId)
    {
        if (request?.Headers.Authorization.Count > 0)
            message.Headers.TryAddWithoutValidation("Authorization", request.Headers.Authorization.ToString());
        if (!string.IsNullOrEmpty(tenantId))
            message.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);
    }
}

public sealed record SessionsSummaryReport(int SessionCount, decimal AvgDurationMinutes, DateTimeOffset From, DateTimeOffset To);
public sealed record AlarmsBySeverityReport(IReadOnlyDictionary<string, int> BySeverity, DateTimeOffset From, DateTimeOffset To);
public sealed record PrescriptionComplianceReport(int CompliantCount, int TotalEvaluated, decimal CompliancePercent, DateTimeOffset From, DateTimeOffset To);
public sealed record TreatmentDurationByPatientReport(IReadOnlyList<PatientDurationSummary> ByPatient, DateTimeOffset From, DateTimeOffset To);
public sealed record PatientDurationSummary(string PatientMrn, int SessionCount, decimal TotalMinutes, decimal AvgMinutesPerSession);
public sealed record ObservationsSummaryReport(IReadOnlyList<ObservationCountByCode> ByCode, DateTimeOffset From, DateTimeOffset To);
public sealed record ObservationCountByCode(string Code, int Count);

internal sealed record AlarmsListResponse(IReadOnlyList<AlarmSummaryDto> Alarms);
internal sealed record AlarmSummaryDto(string? Priority);
