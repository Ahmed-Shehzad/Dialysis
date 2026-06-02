using System.Net;
using Dialysis.BuildingBlocks.ClinicianNotification.Twilio;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Dialysis.BuildingBlocks.ClinicianNotification.Tests;

/// <summary>
/// Verifies the TwilioSmsSender behaviour without hitting a real Twilio account:
/// missing config → diagnostic failure result; happy path → POST against the right
/// URL with Basic auth + form body shape Twilio expects.
/// </summary>
public sealed class TwilioSenderTests
{
    [Fact]
    public async Task Missing_Config_Returns_Diagnostic_Failure_Async()
    {
        var sender = new TwilioSmsSender(
            new SingleClientFactory(new StubHandler()),
            Options.Create(new TwilioSmsOptions()),
            NullLogger<TwilioSmsSender>.Instance);

        var result = await sender.SendAsync(SampleRequest(), CancellationToken.None);

        result.Delivered.ShouldBeFalse();
        result.FailureReason.ShouldNotBeNull().ShouldContain("not configured");
    }

    [Fact]
    public async Task Happy_Path_Posts_To_Accounts_Messages_With_Basic_Auth_Async()
    {
        var handler = new StubHandler
        {
            StatusCode = HttpStatusCode.Created,
            ResponseBody = """{"sid":"SM-test-12345","status":"queued"}""",
        };
        var sender = new TwilioSmsSender(
            new SingleClientFactory(handler),
            Options.Create(new TwilioSmsOptions
            {
                AccountSid = "ACfakesid000000000000000000000000",
                AuthToken = "supersecret",
                FromNumber = "+10000000000",
            }),
            NullLogger<TwilioSmsSender>.Instance);

        var result = await sender.SendAsync(SampleRequest(), CancellationToken.None);

        result.Delivered.ShouldBeTrue();
        result.ProviderMessageId.ShouldBe("SM-test-12345");
        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest!.RequestUri!.AbsolutePath.ShouldContain("/Accounts/ACfakesid000000000000000000000000/Messages.json");
        handler.LastRequest.Headers.Authorization?.Scheme.ShouldBe("Basic");
        handler.LastBody.ShouldContain("From=%2B10000000000");
        handler.LastBody.ShouldContain("To=%2B1");
    }

    private static ClinicianNotificationRequest SampleRequest() => new(
        Channel: "sms",
        Address: "+1234567890",
        Subject: "Chair 4 alarm",
        Body: "HIGH_PRESSURE. Acknowledge in the app.",
        DeepLink: null,
        Priority: NotificationPriority.Critical,
        Metadata: new Dictionary<string, string>());

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public string ResponseBody { get; set; } = "{}";
        public HttpRequestMessage? LastRequest { get; private set; }
        public string LastBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return new HttpResponseMessage(StatusCode)
            {
                Content = new StringContent(ResponseBody),
            };
        }
    }
}
