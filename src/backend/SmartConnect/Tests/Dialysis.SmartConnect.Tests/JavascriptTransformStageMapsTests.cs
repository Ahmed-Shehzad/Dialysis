using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.VariableMaps;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class JavascriptTransformStageMapsTests
{
    [Fact]
    public async Task Reads_Sourcemap_Via_Script_Async()
    {
        var (sp, ctx) = Build(sourceMap: new Dictionary<string, object?> { ["originalFilename"] = "patient-42.txt" });
        sp.GetRequiredService<IFlowExecutionContextAccessor>().Current = ctx;

        var paramsJson = Params("sourceMap.get('originalFilename')");
        var result = await new JavascriptTransformStage(sp).TransformAsync(
            Wrap_Message().WithMetadata(JavascriptTransformStage.ParametersMetadataKey, paramsJson),
            CancellationToken.None);

        Assert.Equal("patient-42.txt", Encoding.UTF8.GetString(result.Payload.Span));
    }

    [Fact]
    public async Task Channelmap_Put_Then_Get_In_Same_Dispatch_Async()
    {
        var (sp, ctx) = Build();
        sp.GetRequiredService<IFlowExecutionContextAccessor>().Current = ctx;

        var paramsJson = Params("channelMap.put('mrn', 'M-001'); channelMap.get('mrn')");
        var result = await new JavascriptTransformStage(sp).TransformAsync(
            Wrap_Message().WithMetadata(JavascriptTransformStage.ParametersMetadataKey, paramsJson),
            CancellationToken.None);

        Assert.Equal("M-001", Encoding.UTF8.GetString(result.Payload.Span));
        Assert.Equal("M-001", ctx.ChannelMap["mrn"]);
    }

    [Fact]
    public async Task Connectormap_Is_Isolated_Per_Route_Ordinal_Async()
    {
        var (sp, ctx) = Build(routeCount: 2);
        sp.GetRequiredService<IFlowExecutionContextAccessor>().Current = ctx;

        // Route 0 writes to its own connector map.
        ctx.SetCurrentRouteOrdinal(0);
        await new JavascriptTransformStage(sp).TransformAsync(
            Wrap_Message().WithMetadata(
                JavascriptTransformStage.ParametersMetadataKey,
                Params("connectorMap.put('shared-key', 'route0-value'); 'ok'")),
            CancellationToken.None);

        // Route 1 reads its own connector map (which is empty).
        ctx.SetCurrentRouteOrdinal(1);
        var r1 = await new JavascriptTransformStage(sp).TransformAsync(
            Wrap_Message().WithMetadata(
                JavascriptTransformStage.ParametersMetadataKey,
                Params("var v = connectorMap.get('shared-key'); (v === null || typeof v === 'undefined') ? 'NIL' : String(v)")),
            CancellationToken.None);

        Assert.Equal("NIL", Encoding.UTF8.GetString(r1.Payload.Span));
        Assert.Equal("route0-value", ctx.ConnectorMaps[0]["shared-key"]);
        Assert.False(ctx.ConnectorMaps[1].ContainsKey("shared-key"));
    }

    [Fact]
    public async Task Responsemap_Read_After_Engine_Populates_It_Async()
    {
        var (sp, ctx) = Build();
        ctx.ResponseMap["route-0"] = new Dictionary<string, object?>
        {
            ["status"] = "success",
            ["payload"] = "abc",
        };
        sp.GetRequiredService<IFlowExecutionContextAccessor>().Current = ctx;

        var result = await new JavascriptTransformStage(sp).TransformAsync(
            Wrap_Message().WithMetadata(
                JavascriptTransformStage.ParametersMetadataKey,
                Params("responseMap.get('route-0').status")),
            CancellationToken.None);

        Assert.Equal("success", Encoding.UTF8.GetString(result.Payload.Span));
    }

    private static (IServiceProvider sp, FlowExecutionContext ctx) Build(
        IReadOnlyDictionary<string, object?>? sourceMap = null,
        int routeCount = 1)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFlowExecutionContextAccessor, FlowExecutionContextAccessor>();
        services.AddSingleton<IVariableMapStore, InMemoryVariableMapStore>();
        var sp = services.BuildServiceProvider();

        var maps = new ConcurrentDictionary<string, object?>[routeCount];
        for (var i = 0; i < routeCount; i++) maps[i] = new ConcurrentDictionary<string, object?>();
        var ctx = new FlowExecutionContext
        {
            SourceMap = sourceMap ?? new Dictionary<string, object?>(),
            ConnectorMaps = maps,
        };
        return (sp, ctx);
    }

    private static string Params(string script) =>
        $$$"""{"script": {{{JsonSerializer.Serialize(script)}}} }""";

    private static IntegrationMessage Wrap_Message() =>
        new()
        {
            Id = Guid.NewGuid(),
            FlowId = Guid.NewGuid(),
            CorrelationId = "c1",
            Payload = Encoding.UTF8.GetBytes("orig"),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
}
