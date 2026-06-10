using System.Text;
using Dialysis.SmartConnect.BuiltInPlugins;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Json;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql;
using Dialysis.SmartConnect.Tests.TestPlugins;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class FlowRuntimeEngineTests
{
    [Fact]
    public async Task Dispatch_Runs_Filters_Transforms_Multiple_Outbounds_And_Ledger_Async()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CapturingOutboundAdapter>();
        services.AddSmartConnectPersistenceForPostgresql(SmartConnectPostgres.NewDatabaseConnectionString());
        services.AddSmartConnectCore();

        await using var sp = services.BuildServiceProvider();
        var registry = sp.GetRequiredService<MutableFlowPluginRegistry>();
        registry.RegisterTransformStage(new Utf8PrefixTransformStage("PRE:"));
        registry.RegisterOutboundAdapter(sp.GetRequiredService<CapturingOutboundAdapter>());

        var flowId = Guid.Parse("00000000-0000-4000-8000-000000000001");
        await Seedstartedflow_Async(
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
        Assert.Equal([0, 1], result.OutboundRoutesAttempted);

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
    public async Task Dispatch_When_Filter_Drops_Stops_Outbounds_And_Ledger_Records_Drop_Async()
    {
        var services = new ServiceCollection();
        services.AddSmartConnectPersistenceForPostgresql(SmartConnectPostgres.NewDatabaseConnectionString());
        services.AddSmartConnectCore();

        await using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<MutableFlowPluginRegistry>().RegisterRouteFilter(new DropAllRouteFilter());

        var flowId = Guid.Parse("00000000-0000-4000-8000-000000000002");
        await Seedstartedflow_Async(
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

    [Fact]
    public async Task Dispatch_Sequential_Stops_After_First_Outbound_Failure_Async()
    {
        var services = new ServiceCollection();
        services.AddSingleton<FailingOutboundAdapter>();
        services.AddSingleton<CapturingOutboundAdapter>();
        services.AddSmartConnectPersistenceForPostgresql(SmartConnectPostgres.NewDatabaseConnectionString());
        services.AddSmartConnectCore();

        await using var sp = services.BuildServiceProvider();
        var registry = sp.GetRequiredService<MutableFlowPluginRegistry>();
        registry.RegisterOutboundAdapter(sp.GetRequiredService<FailingOutboundAdapter>());
        registry.RegisterOutboundAdapter(sp.GetRequiredService<CapturingOutboundAdapter>());

        var flowId = Guid.Parse("00000000-0000-4000-8000-000000000003");
        await Seedstartedflow_Async(
            sp,
            flowId,
            new IntegrationFlowPipelineDefinition
            {
                OutboundRoutesSequential = true,
                RouteFilters = [new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue }],
                OutboundRoutes =
                [
                    new OutboundRouteSlot { OutboundAdapterKind = FailingOutboundAdapter.KindValue },
                    new OutboundRouteSlot { OutboundAdapterKind = CapturingOutboundAdapter.KindValue },
                ],
            });

        await using var scope = sp.CreateAsyncScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IFlowRuntime>();

        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = flowId,
            CorrelationId = "seq-fail",
            Payload = "x"u8.ToArray(),
            PayloadFormat = PayloadFormat.Binary,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        var result = await runtime.DispatchAsync(msg, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal([0], result.OutboundRoutesAttempted);

        var capture = sp.GetRequiredService<CapturingOutboundAdapter>();
        Assert.Empty(capture.Sent);
    }

    [Fact]
    public async Task Dispatch_Sequential_Runs_Second_Route_When_First_Succeeds_Async()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CapturingOutboundAdapter>();
        services.AddSmartConnectPersistenceForPostgresql(SmartConnectPostgres.NewDatabaseConnectionString());
        services.AddSmartConnectCore();

        await using var sp = services.BuildServiceProvider();
        var registry = sp.GetRequiredService<MutableFlowPluginRegistry>();
        registry.RegisterOutboundAdapter(sp.GetRequiredService<CapturingOutboundAdapter>());

        var flowId = Guid.Parse("00000000-0000-4000-8000-000000000004");
        await Seedstartedflow_Async(
            sp,
            flowId,
            new IntegrationFlowPipelineDefinition
            {
                OutboundRoutesSequential = true,
                RouteFilters = [new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue }],
                OutboundRoutes =
                [
                    new OutboundRouteSlot { OutboundAdapterKind = PassThroughOutboundAdapter.KindValue },
                    new OutboundRouteSlot { OutboundAdapterKind = CapturingOutboundAdapter.KindValue },
                ],
            });

        await using var scope = sp.CreateAsyncScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IFlowRuntime>();

        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = flowId,
            CorrelationId = "seq-ok",
            Payload = "y"u8.ToArray(),
            PayloadFormat = PayloadFormat.Binary,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        var result = await runtime.DispatchAsync(msg, CancellationToken.None);
        Assert.True(result.Succeeded);
        Assert.Equal([0, 1], result.OutboundRoutesAttempted);

        var capture = sp.GetRequiredService<CapturingOutboundAdapter>();
        Assert.Single(capture.Sent);
        Assert.Equal(1, capture.Sent[0].Ordinal);
    }

    [Fact]
    public async Task Dispatch_Surfaces_Outbound_Response_Payload_On_Result_Async()
    {
        var services = new ServiceCollection();
        var capturing = new CapturingOutboundAdapter
        {
            ResponseBytes = "MSH|^~\\&|ACK||AE"u8.ToArray(),
        };
        services.AddSingleton(capturing);
        services.AddSmartConnectPersistenceForPostgresql(SmartConnectPostgres.NewDatabaseConnectionString());
        services.AddSmartConnectCore();

        await using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<MutableFlowPluginRegistry>().RegisterOutboundAdapter(capturing);

        var flowId = Guid.Parse("00000000-0000-4000-8000-000000000005");
        await Seedstartedflow_Async(
            sp,
            flowId,
            new IntegrationFlowPipelineDefinition
            {
                RouteFilters = [new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue }],
                OutboundRoutes = [new OutboundRouteSlot { OutboundAdapterKind = CapturingOutboundAdapter.KindValue }],
            });

        await using var scope = sp.CreateAsyncScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IFlowRuntime>();

        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = flowId,
            CorrelationId = "ack",
            Payload = "out"u8.ToArray(),
            PayloadFormat = PayloadFormat.Binary,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        var result = await runtime.DispatchAsync(msg, CancellationToken.None);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ResponsePayload);
        Assert.Contains("AE", Encoding.UTF8.GetString(result.ResponsePayload));
    }

    private static async Task Seedstartedflow_Async(ServiceProvider sp, Guid flowId, IntegrationFlowPipelineDefinition pipeline)
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
