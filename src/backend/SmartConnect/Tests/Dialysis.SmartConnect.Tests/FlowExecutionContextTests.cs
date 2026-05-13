using System.Collections.Concurrent;
using Dialysis.SmartConnect.VariableMaps;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class FlowExecutionContextTests
{
    [Fact]
    public void Default_context_has_empty_maps_and_no_current_route()
    {
        var ctx = new FlowExecutionContext();

        Assert.Empty(ctx.SourceMap);
        Assert.Empty(ctx.ChannelMap);
        Assert.Empty(ctx.ConnectorMaps);
        Assert.Empty(ctx.ResponseMap);
        Assert.Equal(-1, ctx.CurrentRouteOrdinal);
    }

    [Fact]
    public void CurrentConnectorMap_returns_correct_bag_per_ordinal()
    {
        var route0 = new ConcurrentDictionary<string, object?>();
        var route1 = new ConcurrentDictionary<string, object?>();
        var ctx = new FlowExecutionContext
        {
            ConnectorMaps = [route0, route1],
        };

        ctx.SetCurrentRouteOrdinal(0);
        ctx.CurrentConnectorMap["foo"] = "alpha";
        ctx.SetCurrentRouteOrdinal(1);
        ctx.CurrentConnectorMap["foo"] = "beta";

        Assert.Equal("alpha", route0["foo"]);
        Assert.Equal("beta", route1["foo"]);
    }

    [Fact]
    public void CurrentConnectorMap_returns_throwaway_when_no_route_active()
    {
        var route0 = new ConcurrentDictionary<string, object?>();
        var ctx = new FlowExecutionContext { ConnectorMaps = [route0] };

        ctx.SetCurrentRouteOrdinal(-1);
        ctx.CurrentConnectorMap["x"] = "leak?";

        Assert.False(route0.ContainsKey("x"));
    }

    [Fact]
    public async Task AsyncLocal_accessor_isolates_parallel_dispatches()
    {
        var accessor = new FlowExecutionContextAccessor();
        var ctxA = new FlowExecutionContext { ConnectorMaps = [new ConcurrentDictionary<string, object?>()] };
        var ctxB = new FlowExecutionContext { ConnectorMaps = [new ConcurrentDictionary<string, object?>()] };

        var taskA = Task.Run(async () =>
        {
            accessor.Current = ctxA;
            await Task.Delay(20);
            return accessor.Current;
        });
        var taskB = Task.Run(async () =>
        {
            accessor.Current = ctxB;
            await Task.Delay(20);
            return accessor.Current;
        });

        var results = await Task.WhenAll(taskA, taskB);
        Assert.Same(ctxA, results[0]);
        Assert.Same(ctxB, results[1]);
    }
}
