using System.Net;
using Dialysis.SmartConnect.Alerts;
using Dialysis.SmartConnect.Alerts.Actions;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class WebhookAlertActionProviderTests
{
    [Fact]
    public async Task Posts_Rendered_Body_To_Configured_Url_Async()
    {
        var capture = new CapturingHandler();
        var factory = new StubFactory(capture);
        var provider = new WebhookAlertActionProvider(factory);

        var rule = new AlertRule { Id = Guid.CreateVersion7(), Name = "WebhookRule" };
        var evt = new AlertEvent
        {
            Id = Guid.CreateVersion7(),
            RuleId = rule.Id,
            FlowId = Guid.CreateVersion7(),
            ErrorType = AlertErrorType.OutboundFailure,
            ErrorDetail = "timeout",
            OccurredAtUtc = DateTimeOffset.UtcNow,
        };
        var slot = new AlertActionSlot
        {
            Kind = "webhook",
            PropertiesJson = """
            {"url":"https://hooks.example/alert","method":"POST","contentType":"application/json","bodyTemplate":"{\"rule\":\"${ruleName}\",\"err\":\"${errorDetail}\"}","headers":{"X-Source":"sc-${ruleName}"}}
            """,
        };

        var result = await provider.ExecuteAsync(evt, rule, slot, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(capture.LastRequest);
        Assert.Equal("https://hooks.example/alert", capture.LastRequest!.RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, capture.LastRequest.Method);
        Assert.Contains("\"rule\":\"WebhookRule\"", capture.LastBody);
        Assert.Contains("\"err\":\"timeout\"", capture.LastBody);
        Assert.True(capture.LastRequest.Headers.TryGetValues("X-Source", out var src));
        Assert.Equal("sc-WebhookRule", string.Join(",", src!));
    }

    [Fact]
    public async Task Non_2xx_Response_Returns_Failure_Async()
    {
        var capture = new CapturingHandler { Status = HttpStatusCode.InternalServerError, Reason = "boom" };
        var provider = new WebhookAlertActionProvider(new StubFactory(capture));
        var rule = new AlertRule { Id = Guid.CreateVersion7(), Name = "r" };
        var evt = new AlertEvent { Id = Guid.CreateVersion7(), RuleId = rule.Id };
        var slot = new AlertActionSlot { Kind = "webhook", PropertiesJson = """{"url":"https://x"}""" };
        var result = await provider.ExecuteAsync(evt, rule, slot, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Contains("500", result.ErrorDetail ?? string.Empty);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string LastBody { get; private set; } = string.Empty;
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public string Reason { get; set; } = "OK";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
            {
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            return new HttpResponseMessage(Status) { ReasonPhrase = Reason };
        }
    }

    private sealed class StubFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public StubFactory(HttpMessageHandler handler) => _handler = handler;
        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }
}
