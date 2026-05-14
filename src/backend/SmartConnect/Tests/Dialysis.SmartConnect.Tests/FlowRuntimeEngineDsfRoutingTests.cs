using System.Text;
using Dialysis.SmartConnect.BuiltInPlugins;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Json;
using Dialysis.SmartConnect.Tests.TestPlugins;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class FlowRuntimeEngineDsfRoutingTests
{
    [Fact]
    public async Task DSF_source_transform_filters_outbound_routes()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CapturingOutboundAdapter>();
        services.AddSmartConnectPersistenceInMemory(databaseName: $"sc_dsf_{Guid.NewGuid():N}");
        services.AddSmartConnectCore();

        await using var sp = services.BuildServiceProvider();
        var registry = sp.GetRequiredService<MutableFlowPluginRegistry>();
        registry.RegisterOutboundAdapter(sp.GetRequiredService<CapturingOutboundAdapter>());

        var dsfParams = """
        {
          "script": "destinationSet.removeAllExcept(['route-allow']);",
          "availableRouteNames": ["route-allow", "route-deny"]
        }
        """;

        var flowId = Guid.Parse("00000000-0000-4000-8000-000000000077");
        await SeedStartedFlowAsync(sp, flowId, new IntegrationFlowPipelineDefinition
        {
            RouteFilters = [new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue }],
            SourceTransformStages =
            [
                new TransformStageSlot
                {
                    Kind = DestinationSetFilterTransformStage.KindValue,
                    ParametersJson = dsfParams,
                },
            ],
            OutboundRoutes =
            [
                new OutboundRouteSlot
                {
                    OutboundAdapterKind = CapturingOutboundAdapter.KindValue,
                    OutboundParametersJson = """{"routeName":"route-allow"}""",
                },
                new OutboundRouteSlot
                {
                    OutboundAdapterKind = CapturingOutboundAdapter.KindValue,
                    OutboundParametersJson = """{"routeName":"route-deny"}""",
                },
            ],
        });

        await using var scope = sp.CreateAsyncScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IFlowRuntime>();

        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = flowId,
            CorrelationId = "dsf-1",
            Payload = Encoding.UTF8.GetBytes("payload"),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        var result = await runtime.DispatchAsync(msg, CancellationToken.None);

        Assert.True(result.Succeeded);
        var capture = sp.GetRequiredService<CapturingOutboundAdapter>();
        Assert.Single(capture.Sent);
        Assert.Equal(0, capture.Sent[0].Ordinal); // only route 0 (route-allow) ran.
    }

    [Fact]
    public async Task RouteName_falls_back_to_indexed_default_when_not_set()
    {
        // DSF references "route-1" by index-default; only that route should run.
        var dsfParams = """
        {
          "script": "destinationSet.removeAllExcept(['route-1']);",
          "availableRouteNames": ["route-0", "route-1"]
        }
        """;

        var services = new ServiceCollection();
        services.AddSingleton<CapturingOutboundAdapter>();
        services.AddSmartConnectPersistenceInMemory(databaseName: $"sc_dsf_idx_{Guid.NewGuid():N}");
        services.AddSmartConnectCore();

        await using var sp = services.BuildServiceProvider();
        var registry = sp.GetRequiredService<MutableFlowPluginRegistry>();
        registry.RegisterOutboundAdapter(sp.GetRequiredService<CapturingOutboundAdapter>());

        var flowId = Guid.Parse("00000000-0000-4000-8000-000000000078");
        await SeedStartedFlowAsync(sp, flowId, new IntegrationFlowPipelineDefinition
        {
            RouteFilters = [new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue }],
            SourceTransformStages =
            [
                new TransformStageSlot
                {
                    Kind = DestinationSetFilterTransformStage.KindValue,
                    ParametersJson = dsfParams,
                },
            ],
            OutboundRoutes =
            [
                new OutboundRouteSlot { OutboundAdapterKind = CapturingOutboundAdapter.KindValue },
                new OutboundRouteSlot { OutboundAdapterKind = CapturingOutboundAdapter.KindValue },
            ],
        });

        await using var scope = sp.CreateAsyncScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IFlowRuntime>();

        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = flowId,
            CorrelationId = "dsf-idx",
            Payload = Encoding.UTF8.GetBytes("payload"),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        var result = await runtime.DispatchAsync(msg, CancellationToken.None);

        Assert.True(result.Succeeded);
        var capture = sp.GetRequiredService<CapturingOutboundAdapter>();
        Assert.Single(capture.Sent);
        Assert.Equal(1, capture.Sent[0].Ordinal);
    }

    private async static Task SeedStartedFlowAsync(ServiceProvider sp, Guid flowId, IntegrationFlowPipelineDefinition pipeline)
    {
        await using var scope = sp.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();
        db.IntegrationFlows.Add(new IntegrationFlowEntity
        {
            Id = flowId,
            Name = "dsf-test",
            RuntimeState = (int)FlowRuntimeState.Started,
            PipelineJson = PipelineJsonSerializer.Serialize(pipeline),
        });
        await db.SaveChangesAsync();
    }
}
