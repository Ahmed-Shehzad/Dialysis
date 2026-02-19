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
        using var req = new HttpRequestMessage(HttpMethod.Get, baseUrl.TrimEnd('/') + $"/api/treatment-sessions/reports/summary?from={from:O}&to={to:O}");
        AddHeaders(req, request, tenantId);
        HttpResponseMessage response = await _http.SendAsync(req, cancellationToken);
        response.EnsureSuccessStatusCode();
        SessionsSummaryReport? report = await response.Content.ReadFromJsonAsync<SessionsSummaryReport>(cancellationToken);
        return report ?? new SessionsSummaryReport(0, 0, from, to);
    }

    public async Task<AlarmsBySeverityReport> GetAlarmsBySeverityAsync(DateTimeOffset from, DateTimeOffset to, string? tenantId, HttpRequest? request, CancellationToken cancellationToken = default)
    {
        string baseUrl = _config["Reports:BaseUrl"] ?? "http://localhost:5000";
        using var req = new HttpRequestMessage(HttpMethod.Get, baseUrl.TrimEnd('/') + $"/api/alarms?from={from:O}&to={to:O}");
        AddHeaders(req, request, tenantId);
        HttpResponseMessage response = await _http.SendAsync(req, cancellationToken);
        response.EnsureSuccessStatusCode();
        AlarmsListResponse? listResponse = await response.Content.ReadFromJsonAsync<AlarmsListResponse>(cancellationToken);
        IReadOnlyList<AlarmSummaryDto> alarms = listResponse?.Alarms ?? [];
        var bySeverity = alarms
            .GroupBy(a => a.Priority ?? "unknown")
            .ToDictionary(g => g.Key, g => g.Count());
        return new AlarmsBySeverityReport(bySeverity, from, to);
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
        {
            if (await IsSessionCompliantAsync(baseUrl, sessionIds[i], tenantId, request, cancellationToken))
                compliant++;
        }

        decimal percent = toCheck > 0 ? Math.Round(100m * compliant / toCheck, 1) : 0;
        return new PrescriptionComplianceReport(compliant, toCheck, percent, from, to);
    }

    private async Task<IReadOnlyList<string>> GetSessionIdsInRangeAsync(string baseUrl, DateTimeOffset from, DateTimeOffset to, string? tenantId, HttpRequest? request, CancellationToken ct)
    {
        string url = baseUrl.TrimEnd('/') + $"/api/treatment-sessions/fhir?dateFrom={from:O}&dateTo={to:O}&limit=100";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        AddHeaders(req, request, tenantId);
        _ = req.Headers.TryAddWithoutValidation("Accept", "application/fhir+json");
        HttpResponseMessage response = await _http.SendAsync(req, ct);
        if (!response.IsSuccessStatusCode)
            return [];

        string json = await response.Content.ReadAsStringAsync(ct);
        return ExtractSessionIdsFromFhirBundle(json);
    }

    private static IReadOnlyList<string> ExtractSessionIdsFromFhirBundle(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var ids = new List<string>();
            if (doc.RootElement.TryGetProperty("entry", out var entries))
                foreach (var e in entries.EnumerateArray())
                    if (e.TryGetProperty("resource", out var res) && res.TryGetProperty("resourceType", out var rt) && rt.GetString() == "Procedure" && res.TryGetProperty("id", out var idProp))
                    {
                        string id = idProp.GetString() ?? "";
                        if (id.StartsWith("proc-", StringComparison.Ordinal))
                            ids.Add(id["proc-".Length..]);
                    }
            return ids;
        }
        catch
        {
            return [];
        }
    }

    private async Task<bool> IsSessionCompliantAsync(string baseUrl, string sessionId, string? tenantId, HttpRequest? request, CancellationToken ct)
    {
        string url = baseUrl.TrimEnd('/') + $"/api/cds/prescription-compliance?sessionId={Uri.EscapeDataString(sessionId)}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        AddHeaders(req, request, tenantId);
        _ = req.Headers.TryAddWithoutValidation("Accept", "application/fhir+json");
        HttpResponseMessage response = await _http.SendAsync(req, ct);
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

internal sealed record AlarmsListResponse(IReadOnlyList<AlarmSummaryDto> Alarms);
internal sealed record AlarmSummaryDto(string? Priority);
