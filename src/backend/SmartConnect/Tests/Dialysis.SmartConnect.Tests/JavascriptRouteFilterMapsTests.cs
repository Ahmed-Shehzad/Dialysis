using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.VariableMaps;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class JavascriptRouteFilterMapsTests
{
    [Fact]
    public async Task Allows_When_Dollar_Walker_Finds_Key_In_Source_Map_Async()
    {
        var (sp, ctx) = Build(sourceMap: new Dictionary<string, object?> { ["originalFilename"] = "ok.txt" });
        sp.GetRequiredService<IFlowExecutionContextAccessor>().Current = ctx;

        var paramsJson = Params("$('originalFilename') === 'ok.txt'");
        var msg = Wrap_Message().WithMetadata(JavascriptRouteFilter.ParametersMetadataKey, paramsJson);

        var result = await new JavascriptRouteFilter(sp).EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(RouteFilterDisposition.Allow, result.Disposition);
    }

    [Fact]
    public async Task Drops_When_Dollar_Walker_Key_Missing_Async()
    {
        var (sp, ctx) = Build();
        sp.GetRequiredService<IFlowExecutionContextAccessor>().Current = ctx;

        var msg = Wrap_Message().WithMetadata(
            JavascriptRouteFilter.ParametersMetadataKey,
            Params("typeof $('not-set') !== 'undefined'"));

        var result = await new JavascriptRouteFilter(sp).EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(RouteFilterDisposition.Drop, result.Disposition);
    }

    [Fact]
    public async Task Channel_Overrides_Source_When_Walker_Runs_Async()
    {
        var (sp, ctx) = Build(sourceMap: new Dictionary<string, object?> { ["k"] = "lose" });
        ctx.ChannelMap["k"] = "win";
        sp.GetRequiredService<IFlowExecutionContextAccessor>().Current = ctx;

        var msg = Wrap_Message().WithMetadata(
            JavascriptRouteFilter.ParametersMetadataKey,
            Params("$('k') === 'win'"));

        var result = await new JavascriptRouteFilter(sp).EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(RouteFilterDisposition.Allow, result.Disposition);
    }

    private static (IServiceProvider sp, FlowExecutionContext ctx) Build(
        IReadOnlyDictionary<string, object?>? sourceMap = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFlowExecutionContextAccessor, FlowExecutionContextAccessor>();
        services.AddSingleton<IVariableMapStore, InMemoryVariableMapStore>();
        var sp = services.BuildServiceProvider();

        var ctx = new FlowExecutionContext
        {
            SourceMap = sourceMap ?? new Dictionary<string, object?>(),
            ConnectorMaps = [new ConcurrentDictionary<string, object?>()],
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
            Payload = Encoding.UTF8.GetBytes("payload"),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
}
