using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.BuiltInPlugins;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Json;
using Dialysis.SmartConnect.Tests.TestPlugins;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class FlowRuntimeEngineVariableMapsTests
{
    [Fact]
    public async Task SourceMap_metadata_is_hydrated_and_visible_to_transform_script()
    {
        var (sp, capture) = await BuildAsync();
        var flowId = Guid.Parse("00000000-0000-4000-8000-0000000000d1");

        // A simple pipeline: one route, one transform stage that emits the sourceMap value as the new payload.
        var transformScript = JsonSerializer.Serialize(new { script = "sourceMap.get('originalFilename')" });
        await SeedStartedFlowAsync(sp, flowId, new IntegrationFlowPipelineDefinition
        {
            RouteFilters = [new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue }],
            OutboundRoutes =
            [
                new OutboundRouteSlot
                {
                    OutboundAdapterKind = CapturingOutboundAdapter.KindValue,
                    TransformStages =
                    [
                        new TransformStageSlot { Kind = "javascript", ParametersJson = transformScript },
                    ],
                },
            ],
        });

        var sourceMapJson = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["originalFilename"] = "patient-99.txt",
        });

        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = flowId,
            CorrelationId = "vm-1",
            Payload = Encoding.UTF8.GetBytes("ignored"),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        }.WithMetadata(FlowRuntimeEngine.SourceMapMetadataKey, sourceMapJson);

        await using var scope = sp.CreateAsyncScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IFlowRuntime>();
        var result = await runtime.DispatchAsync(msg, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Single(capture.Sent);
        Assert.Equal("patient-99.txt", Encoding.UTF8.GetString(capture.Sent[0].Payload.Span));
    }

    [Fact]
    public async Task ResponseMap_is_auto_populated_and_visible_to_later_route()
    {
        var (sp, capture) = await BuildAsync();
        capture.ResponseBytes = Encoding.UTF8.GetBytes("from-route-0");

        var flowId = Guid.Parse("00000000-0000-4000-8000-0000000000d2");
        // Route 0 just sends; engine captures the adapter response into responseMap["route-0"].
        // Route 1 has a transform script that reads responseMap.get('route-0').status.
        var route1Transform = JsonSerializer.Serialize(new { script = "responseMap.get('route-0').status" });

        await SeedStartedFlowAsync(sp, flowId, new IntegrationFlowPipelineDefinition
        {
            RouteFilters = [new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue }],
            OutboundRoutesSequential = true,
            OutboundRoutes =
            [
                new OutboundRouteSlot { OutboundAdapterKind = CapturingOutboundAdapter.KindValue },
                new OutboundRouteSlot
                {
                    OutboundAdapterKind = CapturingOutboundAdapter.KindValue,
                    TransformStages =
                    [
                        new TransformStageSlot { Kind = "javascript", ParametersJson = route1Transform },
                    ],
                },
            ],
        });

        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = flowId,
            CorrelationId = "vm-2",
            Payload = Encoding.UTF8.GetBytes("orig"),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        await using var scope = sp.CreateAsyncScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IFlowRuntime>();
        var result = await runtime.DispatchAsync(msg, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(2, capture.Sent.Count);
        // Route 0 sent the original payload; route 1 sent the response-map lookup output.
        Assert.Equal("orig", Encoding.UTF8.GetString(capture.Sent[0].Payload.Span));
        Assert.Equal("success", Encoding.UTF8.GetString(capture.Sent[1].Payload.Span));
    }

    private async static Task<(ServiceProvider sp, CapturingOutboundAdapter capture)> BuildAsync()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CapturingOutboundAdapter>();
        services.AddSmartConnectPersistenceInMemory(databaseName: $"sc_vm_{Guid.NewGuid():N}");
        services.AddSmartConnectCore();

        var sp = services.BuildServiceProvider();
        var registry = sp.GetRequiredService<MutableFlowPluginRegistry>();
        var capture = sp.GetRequiredService<CapturingOutboundAdapter>();
        registry.RegisterOutboundAdapter(capture);
        return (sp, capture);
    }

    private async static Task SeedStartedFlowAsync(ServiceProvider sp, Guid flowId, IntegrationFlowPipelineDefinition pipeline)
    {
        await using var scope = sp.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();
        db.IntegrationFlows.Add(new IntegrationFlowEntity
        {
            Id = flowId,
            Name = "vm-test",
            RuntimeState = (int)FlowRuntimeState.Started,
            PipelineJson = PipelineJsonSerializer.Serialize(pipeline),
        });
        await db.SaveChangesAsync();
    }
}
