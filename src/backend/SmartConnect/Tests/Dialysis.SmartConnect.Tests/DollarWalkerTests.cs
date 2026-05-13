using System.Collections.Concurrent;
using System.Text;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.VariableMaps;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Verifies the <c>$()</c> walker's precedence order (Mirth UG p454):
/// Response → Connector → Channel → Source → GlobalChannel → Global → Configuration.
/// Drives the binder indirectly through <see cref="JavascriptTransformStage"/> so visibility stays internal.
/// </summary>
public sealed class DollarWalkerTests
{
    [Fact]
    public Task Returns_value_from_response_when_only_present_there() =>
        AssertWalkerResolves(populate: ctx => ctx.ResponseMap["k"] = "from-response", expected: "from-response");

    [Fact]
    public Task Returns_value_from_connector_when_only_present_there() =>
        AssertWalkerResolves(populate: ctx =>
        {
            ctx.SetCurrentRouteOrdinal(0);
            ctx.CurrentConnectorMap["k"] = "from-connector";
        }, expected: "from-connector");

    [Fact]
    public Task Returns_value_from_channel_when_only_present_there() =>
        AssertWalkerResolves(populate: ctx => ctx.ChannelMap["k"] = "from-channel", expected: "from-channel");

    [Fact]
    public Task Returns_value_from_source_when_only_present_there() =>
        AssertWalkerResolves(
            populate: _ => { },
            sourceMap: new Dictionary<string, object?> { ["k"] = "from-source" },
            expected: "from-source");

    [Fact]
    public Task Response_wins_over_connector_and_channel_and_source() =>
        AssertWalkerResolves(populate: ctx =>
        {
            ctx.SetCurrentRouteOrdinal(0);
            ctx.CurrentConnectorMap["k"] = "lose-conn";
            ctx.ChannelMap["k"] = "lose-channel";
            ctx.ResponseMap["k"] = "win-response";
        }, sourceMap: new Dictionary<string, object?> { ["k"] = "lose-source" }, expected: "win-response");

    [Fact]
    public async Task Returns_undefined_when_key_missing_everywhere()
    {
        var (services, ctx) = BuildServices();
        var accessor = services.GetRequiredService<IFlowExecutionContextAccessor>();
        accessor.Current = ctx;

        var stage = new JavascriptTransformStage(services);
        var script = """
        var v = $('nothing-here');
        (typeof v === 'undefined') ? 'undef' : ('defined:' + v);
        """;
        var paramsJson = $$$"""{"script": {{{System.Text.Json.JsonSerializer.Serialize(script)}}} }""";

        var msg = WrapMessage().WithMetadata(JavascriptTransformStage.ParametersMetadataKey, paramsJson);
        var result = await stage.TransformAsync(msg, CancellationToken.None);

        Assert.Equal("undef", Encoding.UTF8.GetString(result.Payload.Span));
    }

    private static async Task AssertWalkerResolves(
        Action<FlowExecutionContext> populate,
        string expected,
        IReadOnlyDictionary<string, object?>? sourceMap = null)
    {
        var (services, ctx) = BuildServices(sourceMap);
        populate(ctx);
        var accessor = services.GetRequiredService<IFlowExecutionContextAccessor>();
        accessor.Current = ctx;

        var stage = new JavascriptTransformStage(services);
        var script = "var v = $('k'); (v === null || typeof v === 'undefined') ? 'NIL' : String(v);";
        var paramsJson = $$$"""{"script": {{{System.Text.Json.JsonSerializer.Serialize(script)}}} }""";

        var msg = WrapMessage().WithMetadata(JavascriptTransformStage.ParametersMetadataKey, paramsJson);
        var result = await stage.TransformAsync(msg, CancellationToken.None);

        Assert.Equal(expected, Encoding.UTF8.GetString(result.Payload.Span));
    }

    private static (IServiceProvider services, FlowExecutionContext ctx) BuildServices(
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

    private static IntegrationMessage WrapMessage() =>
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
