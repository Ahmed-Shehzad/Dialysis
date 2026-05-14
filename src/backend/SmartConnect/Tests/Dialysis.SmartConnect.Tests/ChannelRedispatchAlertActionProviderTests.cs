using System.Text;
using Dialysis.SmartConnect.Alerts;
using Dialysis.SmartConnect.Alerts.Actions;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class ChannelRedispatchAlertActionProviderTests
{
    [Fact]
    public async Task Dispatches_Message_With_Rendered_Payload_And_Alert_Metadata_Async()
    {
        var runtime = new CapturingFlowRuntime();
        var provider = new ChannelRedispatchAlertActionProvider(() => runtime, TimeProvider.System);

        var rule = new AlertRule { Id = Guid.CreateVersion7(), Name = "redispatch-rule" };
        var evt = new AlertEvent
        {
            Id = Guid.CreateVersion7(),
            RuleId = rule.Id,
            FlowId = Guid.CreateVersion7(),
            ErrorType = AlertErrorType.OutboundFailure,
            ErrorDetail = "broken",
            CorrelationId = "corr-1",
            OccurredAtUtc = DateTimeOffset.UtcNow,
        };
        var targetFlow = Guid.CreateVersion7();
        var slot = new AlertActionSlot
        {
            Kind = "channel-redispatch",
            PropertiesJson = $$"""{"targetFlowId":"{{targetFlow}}","payloadTemplate":"alert ${ruleName} for flow ${flowId}"}""",
        };

        var result = await provider.ExecuteAsync(evt, rule, slot, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(runtime.Last);
        Assert.Equal(targetFlow, runtime.Last!.FlowId);
        Assert.Equal("corr-1", runtime.Last.CorrelationId);
        var body = Encoding.UTF8.GetString(runtime.Last.Payload.Span);
        Assert.Contains("redispatch-rule", body);
        Assert.Contains(evt.FlowId!.Value.ToString(), body);
        Assert.True(runtime.Last.Metadata.ContainsKey("smartconnect.alert.ruleId"));
        Assert.Equal(rule.Id.ToString(), runtime.Last.Metadata["smartconnect.alert.ruleId"]);
        Assert.Equal(evt.Id.ToString(), runtime.Last.Metadata["smartconnect.alert.eventId"]);
        Assert.Equal("OutboundFailure", runtime.Last.Metadata["smartconnect.alert.errorType"]);
    }

    [Fact]
    public async Task Missing_Target_Flow_Id_Returns_Failure_Async()
    {
        var runtime = new CapturingFlowRuntime();
        var provider = new ChannelRedispatchAlertActionProvider(() => runtime, TimeProvider.System);
        var rule = new AlertRule { Id = Guid.CreateVersion7(), Name = "r" };
        var evt = new AlertEvent { Id = Guid.CreateVersion7(), RuleId = rule.Id };
        var result = await provider.ExecuteAsync(evt, rule, new AlertActionSlot { Kind = "channel-redispatch", PropertiesJson = "{}" }, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Null(runtime.Last);
    }

    private sealed class CapturingFlowRuntime : IFlowRuntime
    {
        public IntegrationMessage? Last { get; private set; }
        public Task<FlowDispatchResult> DispatchAsync(IntegrationMessage message, CancellationToken cancellationToken)
        {
            Last = message;
            return Task.FromResult(new FlowDispatchResult { Succeeded = true, OutboundRoutesAttempted = [] });
        }
    }
}
