using Dialysis.SmartConnect.Alerts;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class AlertVariablesTests
{
    [Fact]
    public void Render_substitutes_known_tokens()
    {
        var rule = new AlertRule { Id = Guid.Parse("11111111-1111-4111-8111-111111111111"), Name = "MyRule" };
        var evt = new AlertEvent
        {
            Id = Guid.Parse("22222222-2222-4222-8222-222222222222"),
            RuleId = rule.Id,
            FlowId = Guid.Parse("33333333-3333-4333-8333-333333333333"),
            ErrorType = AlertErrorType.OutboundFailure,
            ErrorDetail = "boom",
            OccurredAtUtc = new DateTimeOffset(2026, 5, 13, 12, 0, 0, TimeSpan.Zero),
        };

        var rendered = AlertVariables.Render("Rule=${ruleName} Err=${errorType}/${errorDetail} alert=${alertId} flow=${flowId} ruleId=${ruleId}", evt, rule);
        Assert.Contains("Rule=MyRule", rendered);
        Assert.Contains("OutboundFailure", rendered);
        Assert.Contains("boom", rendered);
        Assert.Contains(evt.Id.ToString(), rendered);
        Assert.Contains(rule.Id.ToString(), rendered);
        Assert.Contains(evt.FlowId!.Value.ToString(), rendered);
    }

    [Fact]
    public void Unknown_tokens_stay_literal()
    {
        var rule = new AlertRule { Id = Guid.CreateVersion7(), Name = "n" };
        var evt = new AlertEvent { Id = Guid.CreateVersion7(), RuleId = rule.Id };
        var rendered = AlertVariables.Render("a=${unknown} b=${ruleName}", evt, rule);
        Assert.Contains("${unknown}", rendered);
        Assert.Contains("b=n", rendered);
    }

    [Fact]
    public void Null_fields_render_as_empty_string()
    {
        var rule = new AlertRule { Id = Guid.CreateVersion7(), Name = "n" };
        var evt = new AlertEvent { Id = Guid.CreateVersion7(), RuleId = rule.Id, FlowId = null, ErrorDetail = null };
        var rendered = AlertVariables.Render("[${flowId}] [${errorDetail}] [${correlationId}]", evt, rule);
        Assert.Equal("[] [] []", rendered);
    }

    [Fact]
    public void Empty_template_returns_empty()
    {
        var rule = new AlertRule { Id = Guid.CreateVersion7(), Name = "n" };
        var evt = new AlertEvent { Id = Guid.CreateVersion7(), RuleId = rule.Id };
        Assert.Equal("", AlertVariables.Render("", evt, rule));
        Assert.Equal("", AlertVariables.Render(null, evt, rule));
    }
}
