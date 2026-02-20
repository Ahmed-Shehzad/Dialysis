using System.Net;
using System.Net.Http;

using Dialysis.Reports.Api;

using Microsoft.Extensions.Logging;

using Refit;

using Shouldly;

using Xunit;

using Task = System.Threading.Tasks.Task;

namespace Dialysis.Reports.Tests;

/// <summary>
/// Integration-style tests for ReportsAggregationService using a mock IReportsGatewayApi.
/// </summary>
public sealed class ReportsAggregationServiceTests
{
    private static readonly ILogger<ReportsAggregationService> Logger = new LoggerFactory().CreateLogger<ReportsAggregationService>();

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public async Task GetSessionsSummaryAsync_WithMockedResponse_ReturnsReportAsync()
    {
        var api = new MockReportsGatewayApi()
            .SessionSummary("""{"sessionCount":5,"avgDurationMinutes":180.5,"from":"2025-01-01T00:00:00Z","to":"2025-01-08T00:00:00Z"}""");
        var service = new ReportsAggregationService(api, Logger);

        var from = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2025, 1, 8, 0, 0, 0, TimeSpan.Zero);
        SessionsSummaryReport report = await service.GetSessionsSummaryAsync(from, to, "default", null);

        report.SessionCount.ShouldBe(5);
        report.AvgDurationMinutes.ShouldBe(180.5m);
        report.From.ShouldBe(from);
        report.To.ShouldBe(to);
    }

    [Fact]
    public async Task GetAlarmsBySeverityAsync_WithMockedResponse_ReturnsGroupedReportAsync()
    {
        var api = new MockReportsGatewayApi()
            .Alarms("""{"alarms":[{"priority":"high"},{"priority":"high"},{"priority":"low"}]}""");
        var service = new ReportsAggregationService(api, Logger);

        var from = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2025, 1, 8, 0, 0, 0, TimeSpan.Zero);
        AlarmsBySeverityReport report = await service.GetAlarmsBySeverityAsync(from, to, "default", null);

        report.BySeverity["high"].ShouldBe(2);
        report.BySeverity["low"].ShouldBe(1);
    }

    [Fact]
    public async Task GetAlarmsBySeverityAsync_WhenNoAlarms_ReturnsEmptyBySeverityAsync()
    {
        var api = new MockReportsGatewayApi().Alarms("""{"alarms":[]}""");
        var service = new ReportsAggregationService(api, Logger);

        var from = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2025, 1, 8, 0, 0, 0, TimeSpan.Zero);
        AlarmsBySeverityReport report = await service.GetAlarmsBySeverityAsync(from, to, "default", null);

        report.BySeverity.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPrescriptionComplianceAsync_WhenNoSessionsInRange_ReturnsZeroReportAsync()
    {
        var api = new MockReportsGatewayApi()
            .TreatmentSessionsFhir("""{"resourceType":"Bundle","type":"collection","entry":[]}""");
        var service = new ReportsAggregationService(api, Logger);

        var from = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2025, 1, 8, 0, 0, 0, TimeSpan.Zero);
        PrescriptionComplianceReport report = await service.GetPrescriptionComplianceAsync(from, to, "default", null);

        report.CompliantCount.ShouldBe(0);
        report.TotalEvaluated.ShouldBe(0);
        report.CompliancePercent.ShouldBe(0);
    }

    [Fact]
    public async Task GetPrescriptionComplianceAsync_WithSessionsAndCompliantCds_ReturnsComplianceReportAsync()
    {
        var api = new MockReportsGatewayApi()
            .TreatmentSessionsFhir("""{"resourceType":"Bundle","type":"collection","entry":[{"resource":{"resourceType":"Procedure","id":"proc-sess1"}}]}""")
            .PrescriptionCompliance("""{"resourceType":"Bundle","type":"collection","entry":[]}""");
        var service = new ReportsAggregationService(api, Logger);

        var from = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2025, 1, 8, 0, 0, 0, TimeSpan.Zero);
        PrescriptionComplianceReport report = await service.GetPrescriptionComplianceAsync(from, to, "default", null);

        report.TotalEvaluated.ShouldBe(1);
        report.CompliantCount.ShouldBe(1);
        report.CompliancePercent.ShouldBe(100m);
    }

    [Fact]
    public async Task GetAlarmsBySeverityAsync_WhenBackendReturns500_ReturnsEmptyReportAsync()
    {
        var api = new MockReportsGatewayApi().Alarms(status: HttpStatusCode.InternalServerError);
        var service = new ReportsAggregationService(api, Logger);

        var from = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2025, 1, 8, 0, 0, 0, TimeSpan.Zero);
        AlarmsBySeverityReport report = await service.GetAlarmsBySeverityAsync(from, to, "default", null);

        report.BySeverity.ShouldBeEmpty();
        report.From.ShouldBe(from);
        report.To.ShouldBe(to);
    }

    [Fact]
    public async Task GetSessionsSummaryAsync_WhenBackendReturns404_ThrowsAsync()
    {
        var api = new MockReportsGatewayApi().SessionSummary("", HttpStatusCode.NotFound);
        var service = new ReportsAggregationService(api, Logger);

        var from = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2025, 1, 8, 0, 0, 0, TimeSpan.Zero);

        await Should.ThrowAsync<HttpRequestException>(() =>
            service.GetSessionsSummaryAsync(from, to, "default", null));
    }

    private sealed class MockReportsGatewayApi : IReportsGatewayApi
    {
        private string _sessionSummary = "";
        private HttpStatusCode _sessionSummaryStatus = HttpStatusCode.OK;
        private string _alarms = """{"alarms":[]}""";
        private HttpStatusCode _alarmsStatus = HttpStatusCode.OK;
        private string _treatmentSessionsFhir = """{"resourceType":"Bundle","type":"collection","entry":[]}""";
        private HttpStatusCode _treatmentSessionsFhirStatus = HttpStatusCode.OK;
        private string _prescriptionCompliance = """{"resourceType":"Bundle","type":"collection","entry":[]}""";
        private HttpStatusCode _prescriptionComplianceStatus = HttpStatusCode.OK;

        public MockReportsGatewayApi SessionSummary(string body = "", HttpStatusCode status = HttpStatusCode.OK)
        {
            _sessionSummary = body;
            _sessionSummaryStatus = status;
            return this;
        }

        public MockReportsGatewayApi Alarms(string body = """{"alarms":[]}""", HttpStatusCode status = HttpStatusCode.OK)
        {
            _alarms = body;
            _alarmsStatus = status;
            return this;
        }

        public MockReportsGatewayApi TreatmentSessionsFhir(string body, HttpStatusCode status = HttpStatusCode.OK)
        {
            _treatmentSessionsFhir = body;
            _treatmentSessionsFhirStatus = status;
            return this;
        }

        public MockReportsGatewayApi PrescriptionCompliance(string body, HttpStatusCode status = HttpStatusCode.OK)
        {
            _prescriptionCompliance = body;
            _prescriptionComplianceStatus = status;
            return this;
        }

        public Task<SessionsSummaryReport> GetSessionsSummaryAsync(string from, string to, string? authorization, string? tenantId, CancellationToken cancellationToken = default)
        {
            if (_sessionSummaryStatus != HttpStatusCode.OK)
                throw new HttpRequestException($"Response status: {_sessionSummaryStatus}");
            return Task.FromResult(System.Text.Json.JsonSerializer.Deserialize<SessionsSummaryReport>(_sessionSummary, JsonOptions)!);
        }

        public Task<IApiResponse<AlarmsListResponse>> GetAlarmsAsync(string from, string to, string? authorization, string? tenantId, CancellationToken cancellationToken = default)
        {
            var response = new HttpResponseMessage(_alarmsStatus);
            if (_alarmsStatus != HttpStatusCode.OK)
                return Task.FromResult<IApiResponse<AlarmsListResponse>>(new ApiResponse<AlarmsListResponse>(response, null, null!, null));
            var content = System.Text.Json.JsonSerializer.Deserialize<AlarmsListResponse>(_alarms, JsonOptions);
            return Task.FromResult<IApiResponse<AlarmsListResponse>>(new ApiResponse<AlarmsListResponse>(response, content, null!, null));
        }

        public Task<IApiResponse<string>> GetTreatmentSessionsFhirAsync(string dateFrom, string dateTo, int limit, string? authorization, string? tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IApiResponse<string>>(new ApiResponse<string>(
                new HttpResponseMessage(_treatmentSessionsFhirStatus),
                _treatmentSessionsFhirStatus == HttpStatusCode.OK ? _treatmentSessionsFhir : null,
                null!,
                null));

        public Task<IApiResponse<string>> GetPrescriptionComplianceAsync(string sessionId, string? authorization, string? tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IApiResponse<string>>(new ApiResponse<string>(
                new HttpResponseMessage(_prescriptionComplianceStatus),
                _prescriptionComplianceStatus == HttpStatusCode.OK ? _prescriptionCompliance : null,
                null!,
                null));
    }
}
