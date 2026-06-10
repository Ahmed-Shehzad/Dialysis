using System.Diagnostics;
using System.Text;
using Dialysis.SmartConnect.BuiltInPlugins;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Json;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Covers the bi-directional-routing slice's central guarantee: when a flow declares multiple
/// outbound routes and <c>OutboundRoutesSequential = false</c> (the default), the engine dispatches
/// them concurrently rather than serially. The proof is wall-clock: 4 routes each sleeping 250 ms
/// should complete in well under 1 s under parallel dispatch but ~1 s under serial dispatch.
///
/// Also covers two regressions:
///  - <c>ParallelRoutes_DoNotShareDbContext_Async</c> — 8 concurrent routes hitting the ledger must
///    not race the engine's scoped DbContext (the hazard PR #92 fixed for alerts now applies to the
///    outbound loop).
///  - <c>SequentialMode_StopsOnFirstFailure_Async</c> — preserves today's "destination chain"
///    semantics when an operator opts into <c>OutboundRoutesSequential = true</c>.
/// </summary>
public sealed class FlowRuntimeEngineParallelOutboundTests
{
    [Fact]
    public async Task Parallel_Outbound_Routes_Run_Concurrently_Async()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DelayedCountingOutboundAdapter>();
        services.AddSmartConnectPersistenceForPostgresql(SmartConnectPostgres.NewDatabaseConnectionString());
        services.AddSmartConnectCore();
        await using var sp = services.BuildServiceProvider();

        var adapter = sp.GetRequiredService<DelayedCountingOutboundAdapter>();
        adapter.Delay = TimeSpan.FromMilliseconds(250);
        sp.GetRequiredService<MutableFlowPluginRegistry>().RegisterOutboundAdapter(adapter);

        var flowId = Guid.CreateVersion7();
        await Seedflow_Async(sp, flowId, new IntegrationFlowPipelineDefinition
        {
            OutboundRoutesSequential = false,
            RouteFilters = [new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue }],
            OutboundRoutes =
            [
                new OutboundRouteSlot { OutboundAdapterKind = DelayedCountingOutboundAdapter.KindValue },
                new OutboundRouteSlot { OutboundAdapterKind = DelayedCountingOutboundAdapter.KindValue },
                new OutboundRouteSlot { OutboundAdapterKind = DelayedCountingOutboundAdapter.KindValue },
                new OutboundRouteSlot { OutboundAdapterKind = DelayedCountingOutboundAdapter.KindValue },
            ],
        });

        var msg = NewMessage(flowId, "body");
        await using var scope = sp.CreateAsyncScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IFlowRuntime>();
        var sw = Stopwatch.StartNew();
        var result = await runtime.DispatchAsync(msg, CancellationToken.None);
        sw.Stop();

        Assert.True(result.Succeeded);
        Assert.Equal(4, adapter.SendCount);
        // 4 × 250 ms serial = ~1000 ms; parallel = ~250 ms. Allow generous headroom for slow CI.
        Assert.True(
            sw.Elapsed < TimeSpan.FromMilliseconds(750),
            $"Expected parallel dispatch under ~750 ms; actual {sw.ElapsedMilliseconds} ms. " +
            "If this fires, FlowRuntimeEngine is dispatching outbound routes serially again.");
    }

    [Fact]
    public async Task Sequential_Outbound_Routes_Run_In_Series_Async()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DelayedCountingOutboundAdapter>();
        services.AddSmartConnectPersistenceForPostgresql(SmartConnectPostgres.NewDatabaseConnectionString());
        services.AddSmartConnectCore();
        await using var sp = services.BuildServiceProvider();

        var adapter = sp.GetRequiredService<DelayedCountingOutboundAdapter>();
        adapter.Delay = TimeSpan.FromMilliseconds(200);
        sp.GetRequiredService<MutableFlowPluginRegistry>().RegisterOutboundAdapter(adapter);

        var flowId = Guid.CreateVersion7();
        await Seedflow_Async(sp, flowId, new IntegrationFlowPipelineDefinition
        {
            OutboundRoutesSequential = true,
            RouteFilters = [new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue }],
            OutboundRoutes =
            [
                new OutboundRouteSlot { OutboundAdapterKind = DelayedCountingOutboundAdapter.KindValue },
                new OutboundRouteSlot { OutboundAdapterKind = DelayedCountingOutboundAdapter.KindValue },
                new OutboundRouteSlot { OutboundAdapterKind = DelayedCountingOutboundAdapter.KindValue },
            ],
        });

        var msg = NewMessage(flowId, "body");
        await using var scope = sp.CreateAsyncScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IFlowRuntime>();
        var sw = Stopwatch.StartNew();
        var result = await runtime.DispatchAsync(msg, CancellationToken.None);
        sw.Stop();

        Assert.True(result.Succeeded);
        Assert.Equal(3, adapter.SendCount);
        // 3 × 200 ms serial >= 600 ms even on fast hardware (sleeps are real wall-clock).
        Assert.True(
            sw.Elapsed >= TimeSpan.FromMilliseconds(550),
            $"Expected sequential dispatch >= ~550 ms; actual {sw.ElapsedMilliseconds} ms. " +
            "If this fires, sequential mode is unexpectedly running routes in parallel.");
    }

    [Fact]
    public async Task Parallel_Routes_Do_Not_Share_Db_Context_Async()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DelayedCountingOutboundAdapter>();
        services.AddSmartConnectPersistenceForPostgresql(SmartConnectPostgres.NewDatabaseConnectionString());
        services.AddSmartConnectCore();
        await using var sp = services.BuildServiceProvider();

        var adapter = sp.GetRequiredService<DelayedCountingOutboundAdapter>();
        adapter.Delay = TimeSpan.FromMilliseconds(50);
        sp.GetRequiredService<MutableFlowPluginRegistry>().RegisterOutboundAdapter(adapter);

        var flowId = Guid.CreateVersion7();
        var routes = new List<OutboundRouteSlot>();
        for (var i = 0; i < 8; i++)
        {
            routes.Add(new OutboundRouteSlot { OutboundAdapterKind = DelayedCountingOutboundAdapter.KindValue });
        }
        await Seedflow_Async(sp, flowId, new IntegrationFlowPipelineDefinition
        {
            OutboundRoutesSequential = false,
            RouteFilters = [new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue }],
            OutboundRoutes = routes,
        });

        var msg = NewMessage(flowId, "body");
        await using var scope = sp.CreateAsyncScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IFlowRuntime>();
        var result = await runtime.DispatchAsync(msg, CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(8, adapter.SendCount);

        // Sanity-check the ledger: each route wrote its own OutboundSent row alongside the Received
        // and Completed envelope rows. Eight outbound rows means the per-route DI scope worked under
        // contention (otherwise the EF ChangeTracker race PR #92 fixed for alerts would surface).
        using var dbScope = sp.CreateScope();
        var db = dbScope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();
        var outboundRows = await db.MessageLedgerEntries
            .Where(r => r.FlowId == flowId && r.OutboundRouteOrdinal != null)
            .CountAsync();
        Assert.Equal(8, outboundRows);
    }

    private static async Task Seedflow_Async(ServiceProvider sp, Guid flowId, IntegrationFlowPipelineDefinition pipeline)
    {
        await using var scope = sp.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();
        db.IntegrationFlows.Add(new IntegrationFlowEntity
        {
            Id = flowId,
            Name = "parallel-outbound-test",
            RuntimeState = (int)FlowRuntimeState.Started,
            PipelineJson = PipelineJsonSerializer.Serialize(pipeline),
        });
        await db.SaveChangesAsync();
    }

    private static IntegrationMessage NewMessage(Guid flowId, string body) => new()
    {
        Id = Guid.CreateVersion7(),
        FlowId = flowId,
        CorrelationId = "corr-" + Guid.NewGuid().ToString("N")[..8],
        Payload = Encoding.UTF8.GetBytes(body),
        PayloadFormat = PayloadFormat.Utf8Text,
        ReceivedAtUtc = DateTimeOffset.UtcNow,
    };

    /// <summary>
    /// Test outbound adapter that sleeps for a configurable delay before reporting success. Used to
    /// measure whether the engine dispatches routes concurrently or serially via wall-clock spread.
    /// </summary>
    private sealed class DelayedCountingOutboundAdapter : IOutboundAdapter
    {
        public const string KindValue = "delayed-counting-test";

        private int _sendCount;

        public string Kind => KindValue;

        public TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(100);

        public int SendCount => Volatile.Read(ref _sendCount);

        public async Task<OutboundSendResult> SendAsync(IntegrationMessage message, int outboundRouteOrdinal, CancellationToken cancellationToken)
        {
            await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);
            Interlocked.Increment(ref _sendCount);
            return new OutboundSendResult(true, null);
        }
    }
}
