using System.Net.Mail;
using Dialysis.SmartConnect.Alerts;
using Dialysis.SmartConnect.Alerts.Actions;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class EmailAlertActionProviderTests
{
    [Fact]
    public async Task Sends_With_Rendered_Subject_Body_And_Recipients_Async()
    {
        var captured = new CapturingSmtpDeliverer();
        var provider = new EmailAlertActionProvider { Deliverer = captured };

        var rule = new AlertRule { Id = Guid.CreateVersion7(), Name = "Parse failure" };
        var evt = new AlertEvent
        {
            Id = Guid.CreateVersion7(),
            RuleId = rule.Id,
            FlowId = Guid.CreateVersion7(),
            ErrorType = AlertErrorType.TransformError,
            ErrorDetail = "Bad MSH segment",
            OccurredAtUtc = DateTimeOffset.UtcNow,
        };
        var slot = new AlertActionSlot
        {
            Kind = "email",
            PropertiesJson = """
            {"host":"smtp.example","port":587,"from":"alerts@example","to":"oncall@example","subject":"[${ruleName}] ${errorType}","body":"detail=${errorDetail} alertId=${alertId}"}
            """,
        };

        var result = await provider.ExecuteAsync(evt, rule, slot, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(captured.Last);
        Assert.Equal("alerts@example", captured.Last!.From!.Address);
        Assert.Contains(captured.Last!.To, a => a.Address == "oncall@example");
        Assert.Equal("[Parse failure] TransformError", captured.Last!.Subject);
        Assert.Contains("detail=Bad MSH segment", captured.Last!.Body);
        Assert.Contains(evt.Id.ToString(), captured.Last!.Body);
        Assert.Equal("smtp.example", captured.LastHost);
        Assert.Equal(587, captured.LastPort);
    }

    [Fact]
    public async Task Missing_Required_Property_Returns_Failure_Async()
    {
        var provider = new EmailAlertActionProvider { Deliverer = new CapturingSmtpDeliverer() };
        var rule = new AlertRule { Id = Guid.CreateVersion7(), Name = "r" };
        var evt = new AlertEvent { Id = Guid.CreateVersion7(), RuleId = rule.Id };
        var result = await provider.ExecuteAsync(evt, rule, new AlertActionSlot { Kind = "email", PropertiesJson = """{"host":"x"}""" }, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.NotNull(result.ErrorDetail);
    }

    private sealed class CapturingSmtpDeliverer : EmailAlertActionProvider.ISmtpDeliverer
    {
        public MailMessage? Last { get; private set; }
        public string? LastHost { get; private set; }
        public int LastPort { get; private set; }

        public Task SendAsync(MailMessage message, string host, int port, CancellationToken cancellationToken)
        {
            Last = message;
            LastHost = host;
            LastPort = port;
            return Task.CompletedTask;
        }
    }
}
