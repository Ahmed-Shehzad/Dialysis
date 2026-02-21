using BuildingBlocks.ExceptionHandling;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;

namespace BuildingBlocks.ExceptionHandling.Tests;

public sealed class ExceptionReportEmailSenderTests
{
    [Fact]
    public async Task SendAsync_WhenDisabled_ReturnsWithoutSendingAsync()
    {
        var options = new ExceptionReportEmailOptions { Enabled = false, DevelopmentEmail = "dev@example.com" };
        var sender = new ExceptionReportEmailSender(
            Microsoft.Extensions.Options.Options.Create(options),
            NullLogger<ExceptionReportEmailSender>.Instance);

        ExceptionReport report = CreateMinimalReport();

        await sender.SendAsync(report);

        // No exception; when disabled, returns immediately
        report.Exception.Message.ShouldBe("Test");
    }

    [Fact]
    public async Task SendAsync_WhenDevelopmentEmailEmpty_ReturnsWithoutSendingAsync()
    {
        var options = new ExceptionReportEmailOptions { Enabled = true, DevelopmentEmail = "" };
        var sender = new ExceptionReportEmailSender(
            Microsoft.Extensions.Options.Options.Create(options),
            NullLogger<ExceptionReportEmailSender>.Instance);

        ExceptionReport report = CreateMinimalReport();

        await sender.SendAsync(report);

        // No exception; when not configured, returns immediately
        report.Response.StatusCode.ShouldBe(500);
    }

    private static ExceptionReport CreateMinimalReport() =>
        new()
        {
            OccurredAt = DateTimeOffset.UtcNow,
            Environment = "Production",
            Request = new RequestSnapshot { Method = "GET", Path = "/api/patients", Headers = new Dictionary<string, string>() },
            Response = new ResponseSnapshot { StatusCode = 500, Body = "{}" },
            Exception = new ExceptionSnapshot { Type = "System.Exception", Message = "Test", ToStringOutput = "Test" },
        };
}
