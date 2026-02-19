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
        var response = await _http.SendAsync(req, cancellationToken);
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<SessionsSummaryReport>(cancellationToken);
        return report ?? new SessionsSummaryReport(0, 0, from, to);
    }

    public async Task<AlarmsBySeverityReport> GetAlarmsBySeverityAsync(DateTimeOffset from, DateTimeOffset to, string? tenantId, HttpRequest? request, CancellationToken cancellationToken = default)
    {
        string baseUrl = _config["Reports:BaseUrl"] ?? "http://localhost:5000";
        using var req = new HttpRequestMessage(HttpMethod.Get, baseUrl.TrimEnd('/') + $"/api/alarms?from={from:O}&to={to:O}");
        AddHeaders(req, request, tenantId);
        var response = await _http.SendAsync(req, cancellationToken);
        response.EnsureSuccessStatusCode();
        var listResponse = await response.Content.ReadFromJsonAsync<AlarmsListResponse>(cancellationToken);
        var alarms = listResponse?.Alarms ?? [];
        var bySeverity = alarms
            .GroupBy(a => a.Priority ?? "unknown")
            .ToDictionary(g => g.Key, g => g.Count());
        return new AlarmsBySeverityReport(bySeverity, from, to);
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

internal sealed record AlarmsListResponse(IReadOnlyList<AlarmSummaryDto> Alarms);
internal sealed record AlarmSummaryDto(string? Priority);
