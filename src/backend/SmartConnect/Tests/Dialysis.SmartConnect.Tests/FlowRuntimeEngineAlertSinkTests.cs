using System.Text;
using Dialysis.SmartConnect.Alerts;
using Dialysis.SmartConnect.BuiltInPlugins;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Json;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class FlowRuntimeEngineAlertSinkTests
{
    [Fact]
    public async Task Outbound_Failure_Fires_Matching_Rule_And_Records_Event_Async()
    {
        var sp = Build_Services(out var actionInvocations);
        var flowId = Guid.CreateVersion7();
        await Seedflow_Async(sp, flowId, new IntegrationFlowPipelineDefinition
        {
            RouteFilters = [new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue }],
            OutboundRoutes =
            [
                // Adapter kind doesn't exist — guaranteed OutboundFailed in the runtime.
                new OutboundRouteSlot { OutboundAdapterKind = "no-such-adapter" },
            ],
        });

        await using var scope = sp.CreateAsyncScope();
        var rules = scope.ServiceProvider.GetRequiredService<IAlertRuleRepository>();
        await rules.UpsertAsync(new AlertRule
        {
            Id = Guid.CreateVersion7(),
            Name = "any-outbound-failure",
            Enabled = true,
            ErrorPatterns = [new AlertErrorPattern { ErrorType = AlertErrorType.OutboundFailure }],
            Actions = [new AlertActionSlot { Kind = "test-record" }],
        }, CancellationToken.None);

        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = flowId,
            CorrelationId = "c1",
            Payload = Encoding.UTF8.GetBytes("x"),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
        var runtime = scope.ServiceProvider.GetRequiredService<IFlowRuntime>();
        await runtime.DispatchAsync(msg, CancellationToken.None);

        // The publish is fire-and-forget. Poll briefly for the event to land.
        var store = scope.ServiceProvider.GetRequiredService<IAlertEventStore>();
        IReadOnlyList<AlertEvent> events = [];
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            events = await store.GetRecentAsync(10, CancellationToken.None);
            if (events.Count > 0)
                break;
            await Task.Delay(50);
        }

        Assert.Single(events);
        Assert.Equal(AlertErrorType.OutboundFailure, events[0].ErrorType);
        Assert.Single(events[0].ActionOutcomes);
        Assert.True(events[0].ActionOutcomes[0].Succeeded);
        Assert.Contains("any-outbound-failure", actionInvocations);
    }

    private static ServiceProvider Build_Services(out List<string> invocations)
    {
        var captured = new List<string>();
        invocations = captured;

        var services = new ServiceCollection();
        services.AddSmartConnectPersistenceForPostgresql(SmartConnectPostgres.NewDatabaseConnectionString());
        services.AddSmartConnectCore();
        var sp = services.BuildServiceProvider();

        sp.GetRequiredService<MutableFlowPluginRegistry>().RegisterAlertActionProvider(new RecordingAlertActionProvider(captured));
        return sp;
    }

    private static async Task Seedflow_Async(ServiceProvider sp, Guid flowId, IntegrationFlowPipelineDefinition pipeline)
    {
        await using var scope = sp.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();
        db.IntegrationFlows.Add(new IntegrationFlowEntity
        {
            Id = flowId,
            Name = "alert-e2e",
            RuntimeState = (int)FlowRuntimeState.Started,
            PipelineJson = PipelineJsonSerializer.Serialize(pipeline),
        });
        await db.SaveChangesAsync();
    }

    private sealed class RecordingAlertActionProvider : IAlertActionProvider
    {
        private readonly List<string> _invocations;
        public RecordingAlertActionProvider(List<string> invocations) => _invocations = invocations;
        public string Kind => "test-record";
        public Task<AlertActionResult> ExecuteAsync(AlertEvent evt, AlertRule rule, AlertActionSlot slot, CancellationToken ct)
        {
            _invocations.Add(rule.Name);
            return Task.FromResult(AlertActionResult.Success("recorded"));
        }
    }
}
