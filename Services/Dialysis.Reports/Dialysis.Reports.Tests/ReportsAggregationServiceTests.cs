using System.Net;

using Dialysis.Reports.Api;

using Microsoft.Extensions.Configuration;

using Shouldly;

using Xunit;

using Task = System.Threading.Tasks.Task;

namespace Dialysis.Reports.Tests;

/// <summary>
/// Integration-style tests for ReportsAggregationService using a mock HttpClient.
/// </summary>
public sealed class ReportsAggregationServiceTests
{
    [Fact]
    public async Task GetSessionsSummaryAsync_WithMockedResponse_ReturnsReportAsync()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Reports:BaseUrl"] = "http://localhost:5000" })
            .Build();
        var handler = new MockReportHttpHandler(req =>
        {
            if (req.RequestUri?.AbsoluteUri.Contains("reports/summary") == true)
                return (HttpStatusCode.OK, """{"sessionCount":5,"avgDurationMinutes":180.5,"from":"2025-01-01T00:00:00Z","to":"2025-01-08T00:00:00Z"}""");
            return (HttpStatusCode.NotFound, "");
        });
        using var client = new HttpClient(handler);

        var service = new ReportsAggregationService(client, config);

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
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Reports:BaseUrl"] = "http://localhost:5000" })
            .Build();
        var handler = new MockReportHttpHandler(req =>
        {
            if (req.RequestUri?.AbsoluteUri.Contains("/api/alarms") == true)
                return (HttpStatusCode.OK, """{"alarms":[{"priority":"high"},{"priority":"high"},{"priority":"low"}]}""");
            return (HttpStatusCode.NotFound, "");
        });
        using var client = new HttpClient(handler);

        var service = new ReportsAggregationService(client, config);

        var from = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2025, 1, 8, 0, 0, 0, TimeSpan.Zero);
        AlarmsBySeverityReport report = await service.GetAlarmsBySeverityAsync(from, to, "default", null);

        report.BySeverity["high"].ShouldBe(2);
        report.BySeverity["low"].ShouldBe(1);
    }

    [Fact]
    public async Task GetAlarmsBySeverityAsync_WhenNoAlarms_ReturnsEmptyBySeverityAsync()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Reports:BaseUrl"] = "http://localhost:5000" })
            .Build();
        var handler = new MockReportHttpHandler(req =>
        {
            if (req.RequestUri?.AbsoluteUri.Contains("/api/alarms") == true)
                return (HttpStatusCode.OK, """{"alarms":[]}""");
            return (HttpStatusCode.NotFound, "");
        });
        using var client = new HttpClient(handler);

        var service = new ReportsAggregationService(client, config);

        var from = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2025, 1, 8, 0, 0, 0, TimeSpan.Zero);
        AlarmsBySeverityReport report = await service.GetAlarmsBySeverityAsync(from, to, "default", null);

        report.BySeverity.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPrescriptionComplianceAsync_WhenNoSessionsInRange_ReturnsZeroReportAsync()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Reports:BaseUrl"] = "http://localhost:5000" })
            .Build();
        var handler = new MockReportHttpHandler(req =>
        {
            if (req.RequestUri?.AbsoluteUri.Contains("treatment-sessions/fhir") == true)
                return (HttpStatusCode.OK, """{"resourceType":"Bundle","type":"collection","entry":[]}""");
            return (HttpStatusCode.NotFound, "");
        });
        using var client = new HttpClient(handler);

        var service = new ReportsAggregationService(client, config);

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
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Reports:BaseUrl"] = "http://localhost:5000" })
            .Build();
        var handler = new MockReportHttpHandler(req =>
        {
            string? uri = req.RequestUri?.AbsoluteUri;
            if (uri != null && uri.Contains("treatment-sessions/fhir"))
                return (HttpStatusCode.OK, """{"resourceType":"Bundle","type":"collection","entry":[{"resource":{"resourceType":"Procedure","id":"proc-sess1"}}]}""");
            if (uri != null && uri.Contains("cds/prescription-compliance"))
                return (HttpStatusCode.OK, """{"resourceType":"Bundle","type":"collection","entry":[]}""");
            return (HttpStatusCode.NotFound, "");
        });
        using var client = new HttpClient(handler);

        var service = new ReportsAggregationService(client, config);

        var from = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2025, 1, 8, 0, 0, 0, TimeSpan.Zero);
        PrescriptionComplianceReport report = await service.GetPrescriptionComplianceAsync(from, to, "default", null);

        report.TotalEvaluated.ShouldBe(1);
        report.CompliantCount.ShouldBe(1);
        report.CompliancePercent.ShouldBe(100m);
    }

    [Fact]
    public async Task GetSessionsSummaryAsync_WhenBackendReturns404_ThrowsAsync()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Reports:BaseUrl"] = "http://localhost:5000" })
            .Build();
        var handler = new MockReportHttpHandler(_ => (HttpStatusCode.NotFound, ""));
        using var client = new HttpClient(handler);

        var service = new ReportsAggregationService(client, config);

        var from = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2025, 1, 8, 0, 0, 0, TimeSpan.Zero);

        await Should.ThrowAsync<HttpRequestException>(() =>
            service.GetSessionsSummaryAsync(from, to, "default", null));
    }

    private sealed class MockReportHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, (HttpStatusCode status, string body)> _responder;

        public MockReportHttpHandler(Func<HttpRequestMessage, (HttpStatusCode status, string body)> responder) =>
            _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            (HttpStatusCode status, string body) = _responder(request);
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }
}
