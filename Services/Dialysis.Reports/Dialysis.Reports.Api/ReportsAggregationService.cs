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
            var byPatient = ParseDurationByPatientFromFhirBundle(json);
            return new TreatmentDurationByPatientReport(byPatient, from, to);
        }
    }

    private static IReadOnlyList<PatientDurationSummary> ParseDurationByPatientFromFhirBundle(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var durations = new Dictionary<string, List<double>>();
            if (!doc.RootElement.TryGetProperty("entry", out var entries))
                return [];
            foreach (var e in entries.EnumerateArray())
            {
                if (!e.TryGetProperty("resource", out var res))
                    continue;
                if (!res.TryGetProperty("resourceType", out var rt) || rt.GetString() != "Procedure")
                    continue;
                string? patientRef = null;
                if (res.TryGetProperty("subject", out var sub) && sub.TryGetProperty("reference", out var refEl))
                    patientRef = refEl.GetString();
                if (string.IsNullOrEmpty(patientRef) || !patientRef.StartsWith("Patient/", StringComparison.Ordinal))
                    continue;
                string mrn = patientRef["Patient/".Length..];
                if (mrn == "unknown") continue;
                double minutes = 0;
                if (res.TryGetProperty("performedPeriod", out var perf))
                {
                    if (perf.TryGetProperty("start", out var startEl) && perf.TryGetProperty("end", out var endEl))
                    {
                        if (DateTimeOffset.TryParse(startEl.GetString(), System.Globalization.CultureInfo.InvariantCulture, out var start) && DateTimeOffset.TryParse(endEl.GetString(), System.Globalization.CultureInfo.InvariantCulture, out var end))
                            minutes = (end - start).TotalMinutes;
                    }
                }
                if (!durations.TryGetValue(mrn, out var list))
                {
                    list = [];
                    durations[mrn] = list;
                }
                list.Add(minutes);
            }
            return durations.Select(kv => new PatientDurationSummary(kv.Key, kv.Value.Count, (decimal)kv.Value.Sum(), (decimal)(kv.Value.Count > 0 ? kv.Value.Average() : 0))).ToList();
        }
        catch
        {
            return [];
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
            var byCode = ParseObservationsByCodeFromFhirBundle(json, codeFilter);
            return new ObservationsSummaryReport(byCode, from, to);
        }
    }

    private static IReadOnlyList<ObservationCountByCode> ParseObservationsByCodeFromFhirBundle(string json, string? codeFilter)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var counts = new Dictionary<string, int>();
            if (!doc.RootElement.TryGetProperty("entry", out var entries))
                return [];
            foreach (var e in entries.EnumerateArray())
            {
                if (!e.TryGetProperty("resource", out var res))
                    continue;
                if (!res.TryGetProperty("resourceType", out var rt) || rt.GetString() != "Observation")
                    continue;
                string? obsCode = null;
                if (res.TryGetProperty("code", out var codeEl) && codeEl.TryGetProperty("coding", out var codings))
                    foreach (var c in codings.EnumerateArray())
                        if (c.TryGetProperty("code", out var codeProp))
                        {
                            obsCode = codeProp.GetString();
                            break;
                        }
                if (string.IsNullOrEmpty(obsCode)) obsCode = "unknown";
                if (!string.IsNullOrEmpty(codeFilter) && !obsCode.Contains(codeFilter, StringComparison.OrdinalIgnoreCase))
                    continue;
                counts.TryGetValue(obsCode, out int count);
                counts[obsCode] = count + 1;
            }
            return counts.OrderByDescending(kv => kv.Value).Select(kv => new ObservationCountByCode(kv.Key, kv.Value)).ToList();
        }
        catch
        {
            return [];
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
