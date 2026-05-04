using System.Text;
using Dialysis.SmartConnect.BuiltInPlugins;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Json;
using Dialysis.SmartConnect.Tests.TestPlugins;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class FlowRuntimeEngineTests
{
    [Fact]
    public async Task Dispatch_runs_filters_transforms_multiple_outbounds_and_ledger()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CapturingOutboundAdapter>();
        services.AddSmartConnectPersistenceInMemory(databaseName: $"sc_test_{Guid.NewGuid():N}");
        services.AddSmartConnectCore();

        await using var sp = services.BuildServiceProvider();
        var registry = sp.GetRequiredService<MutableFlowPluginRegistry>();
        registry.RegisterTransformStage(new Utf8PrefixTransformStage("PRE:"));
        registry.RegisterOutboundAdapter(sp.GetRequiredService<CapturingOutboundAdapter>());

        var flowId = Guid.Parse("00000000-0000-4000-8000-000000000001");
        await SeedStartedFlowAsync(
            sp,
            flowId,
            new IntegrationFlowPipelineDefinition
            {
                RouteFilters = [new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue }],
                OutboundRoutes =
                [
                    new OutboundRouteSlot
                    {
                        OutboundAdapterKind = PassThroughOutboundAdapter.KindValue,
                        TransformStages = [new TransformStageSlot { Kind = Utf8PrefixTransformStage.KindValue }],
                    },
                    new OutboundRouteSlot
                    {
                        OutboundAdapterKind = CapturingOutboundAdapter.KindValue,
                        TransformStages = [new TransformStageSlot { Kind = Utf8PrefixTransformStage.KindValue }],
                    },
                ],
            });

        await using var scope = sp.CreateAsyncScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IFlowRuntime>();
        var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();

        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = flowId,
            CorrelationId = "corr-1",
            Payload = Encoding.UTF8.GetBytes("body"),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        var result = await runtime.DispatchAsync(msg, CancellationToken.None);
        Assert.True(result.Succeeded);
        Assert.Equal(new[] { 0, 1 }, result.OutboundRoutesAttempted);

        var capture = sp.GetRequiredService<CapturingOutboundAdapter>();
        Assert.Single(capture.Sent);
        Assert.Equal(1, capture.Sent[0].Ordinal);
        Assert.Equal("PRE:body", Encoding.UTF8.GetString(capture.Sent[0].Payload.Span));

        var ledger = await db.MessageLedgerEntries.AsNoTracking().OrderBy(e => e.CreatedAtUtc).ToListAsync();
        Assert.Contains(ledger, e => e.Status == (int)MessageLedgerStatus.Received);
        Assert.Contains(ledger, e => e.Status == (int)MessageLedgerStatus.OutboundSent && e.OutboundRouteOrdinal == 0);
        Assert.Contains(ledger, e => e.Status == (int)MessageLedgerStatus.OutboundSent && e.OutboundRouteOrdinal == 1);
        Assert.Contains(ledger, e => e.Status == (int)MessageLedgerStatus.Completed);
    }

    [Fact]
    public async Task Dispatch_when_filter_drops_stops_outbounds_and_ledger_records_drop()
    {
        var services = new ServiceCollection();
        services.AddSmartConnectPersistenceInMemory(databaseName: $"sc_test_{Guid.NewGuid():N}");
        services.AddSmartConnectCore();

        await using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<MutableFlowPluginRegistry>().RegisterRouteFilter(new DropAllRouteFilter());

        var flowId = Guid.Parse("00000000-0000-4000-8000-000000000002");
        await SeedStartedFlowAsync(
            sp,
            flowId,
            new IntegrationFlowPipelineDefinition
            {
                RouteFilters =
                [
                    new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue },
                    new RouteFilterSlot { Kind = DropAllRouteFilter.KindValue },
                ],
                OutboundRoutes =
                [
                    new OutboundRouteSlot { OutboundAdapterKind = PassThroughOutboundAdapter.KindValue },
                ],
            });

        await using var scope = sp.CreateAsyncScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IFlowRuntime>();
        var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();

        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = flowId,
            CorrelationId = "corr-drop",
            Payload = "x"u8.ToArray(),
            PayloadFormat = PayloadFormat.Binary,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        var result = await runtime.DispatchAsync(msg, CancellationToken.None);
        Assert.True(result.Succeeded);
        Assert.Empty(result.OutboundRoutesAttempted);

        var ledger = await db.MessageLedgerEntries.AsNoTracking().ToListAsync();
        Assert.Contains(ledger, e => e.Status == (int)MessageLedgerStatus.RouteFilterDropped);
    }

    private static async Task SeedStartedFlowAsync(ServiceProvider sp, Guid flowId, IntegrationFlowPipelineDefinition pipeline)
    {
        await using var scope = sp.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();
        db.IntegrationFlows.Add(
            new IntegrationFlowEntity
            {
                Id = flowId,
                Name = "test-flow",
                RuntimeState = (int)FlowRuntimeState.Started,
                PipelineJson = PipelineJsonSerializer.Serialize(pipeline),
            });
        await db.SaveChangesAsync();
    }
}
