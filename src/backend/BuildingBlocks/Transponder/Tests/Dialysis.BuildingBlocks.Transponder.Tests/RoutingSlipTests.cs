using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.BuildingBlocks.Transponder.Tests;

[Collection(nameof(RoutingSlipTestCollection))]
public sealed class RoutingSlipTests
{
    [Fact]
    public void Serializer_round_trips_routing_slip_state_with_completed_entries()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageSerializer, SystemTextJsonMessageSerializer>();
        using var provider = services.BuildServiceProvider();
        var serializer = provider.GetRequiredService<IMessageSerializer>();

        var state = new TransponderRoutingSlipState();
        state.Itinerary.Add(new TransponderRoutingSlipActivityRef { Name = "X" });
        state.Itinerary.Add(new TransponderRoutingSlipActivityRef { Name = "Y" });
        state.CurrentIndex = 1;
        state.CompletedActivities.Add(new TransponderRoutingSlipCompletedActivityEntry { Index = 0, Name = "X" });

        var bytes = serializer.Serialize(state);
        var parsed = serializer.Deserialize(typeof(TransponderRoutingSlipState), bytes) as TransponderRoutingSlipState;

        Assert.NotNull(parsed);
        Assert.Equal(1, parsed.CurrentIndex);
        Assert.Single(parsed.CompletedActivities);
        Assert.Equal("X", parsed.CompletedActivities[0].Name);
    }

    [Fact]
    public void Serializer_round_trips_slip_state_after_first_step_with_two_itinerary_entries()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageSerializer, SystemTextJsonMessageSerializer>();
        using var provider = services.BuildServiceProvider();
        var serializer = provider.GetRequiredService<IMessageSerializer>();

        var state = new TransponderRoutingSlipState();
        state.Itinerary.Add(new TransponderRoutingSlipActivityRef { Name = "A" });
        state.Itinerary.Add(new TransponderRoutingSlipActivityRef { Name = "B" });
        state.CurrentIndex = 1;
        state.CompletedActivities.Add(new TransponderRoutingSlipCompletedActivityEntry { Index = 0, Name = "A" });

        var bytes = serializer.Serialize(state);
        var parsed = serializer.Deserialize(typeof(TransponderRoutingSlipState), bytes) as TransponderRoutingSlipState;

        Assert.NotNull(parsed);
        Assert.Equal(2, parsed.Itinerary.Count);
        Assert.Equal(1, parsed.CurrentIndex);
        Assert.Single(parsed.CompletedActivities);
    }

    [Fact]
    public async Task Start_runs_activities_in_order_then_removes_saga_row()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTransponder(b =>
        {
            b.AddRoutingSlipActivity<RoutingSlipTestActivityA>();
            b.AddRoutingSlipActivity<RoutingSlipTestActivityB>();
        });

        await using var provider = services.BuildServiceProvider();
        var starter = provider.GetRequiredService<ITransponderRoutingSlipStarter>();
        var store = provider.GetRequiredService<ITransponderSagaStore>();

        RoutingSlipTestActivityA.Executed.Clear();
        RoutingSlipTestActivityB.Executed.Clear();

        var itinerary = new[]
        {
            new TransponderRoutingSlipActivityRef { Name = nameof(RoutingSlipTestActivityA) },
            new TransponderRoutingSlipActivityRef { Name = nameof(RoutingSlipTestActivityB) },
        };

        var slipId = await starter.StartAsync(itinerary);

        Assert.Single(RoutingSlipTestActivityA.Executed);
        Assert.Single(RoutingSlipTestActivityB.Executed);
        Assert.Equal(nameof(RoutingSlipTestActivityA), RoutingSlipTestActivityA.Executed[0]);
        Assert.Equal(nameof(RoutingSlipTestActivityB), RoutingSlipTestActivityB.Executed[0]);

        var row = await store.GetAsync(TransponderRoutingSlipPersistenceKind.SagaKind, slipId);
        Assert.Null(row);
    }

    [Fact]
    public async Task Continue_with_stale_step_index_is_ignored()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTransponder(b =>
        {
            b.AddRoutingSlipActivity<RoutingSlipTestActivityA>();
            b.AddRoutingSlipActivity<RoutingSlipTestActivityB>();
        });

        await using var provider = services.BuildServiceProvider();
        var starter = provider.GetRequiredService<ITransponderRoutingSlipStarter>();
        var bus = provider.GetRequiredService<ITransponderBus>();

        RoutingSlipTestActivityA.Executed.Clear();
        RoutingSlipTestActivityB.Executed.Clear();

        var slipId = await starter.StartAsync(
            [
                new TransponderRoutingSlipActivityRef { Name = nameof(RoutingSlipTestActivityA) },
                new TransponderRoutingSlipActivityRef { Name = nameof(RoutingSlipTestActivityB) },
            ]);

        await bus.PublishAsync(
            new TransponderRoutingSlipContinue { SlipId = slipId, StepIndex = 0 },
            new TransponderPublishOptions(DeduplicationId: $"{slipId}:step-0-stale"));

        Assert.Single(RoutingSlipTestActivityA.Executed);
        Assert.Single(RoutingSlipTestActivityB.Executed);
    }

    private sealed class RoutingSlipTestActivityA : IRoutingSlipActivity
    {
        public static readonly List<string> Executed = [];

        public string Name => nameof(RoutingSlipTestActivityA);

        public Task ExecuteAsync(IRoutingSlipActivityContext context, CancellationToken cancellationToken = default)
        {
            lock (Executed)
                Executed.Add(Name);
            return Task.CompletedTask;
        }
    }

    private sealed class RoutingSlipTestActivityB : IRoutingSlipActivity
    {
        public static readonly List<string> Executed = [];

        public string Name => nameof(RoutingSlipTestActivityB);

        public Task ExecuteAsync(IRoutingSlipActivityContext context, CancellationToken cancellationToken = default)
        {
            lock (Executed)
                Executed.Add(Name);
            return Task.CompletedTask;
        }
    }
}
