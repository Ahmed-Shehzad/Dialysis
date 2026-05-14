using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.BuildingBlocks.Transponder.Tests;

[Collection(nameof(RoutingSlipTestCollection))]
public sealed class RoutingSlipEventTests
{
    private static readonly object _Log_Lock = new();

    private static readonly List<string> _Event_Log = [];

    [Fact]
    public async Task Happy_Path_Emits_Activity_Completed_Then_Slip_Completed_Async()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTransponder(b =>
        {
            b.AddRoutingSlipActivity<RoutingSlipEventOkA>();
            b.AddRoutingSlipActivity<RoutingSlipEventOkB>();
            b.AddConsumer<TransponderRoutingSlipActivityCompleted, ActivityCompletedRecorder>();
            b.AddConsumer<TransponderRoutingSlipCompleted, SlipCompletedRecorder>();
        });

        await using var provider = services.BuildServiceProvider();
        await using (var scope = provider.CreateAsyncScope())
        {
            var activityConsumers = scope.ServiceProvider.GetServices<IConsumer<TransponderRoutingSlipActivityCompleted>>().ToList();
            Assert.True(activityConsumers.Count >= 2, $"Expected discarding + recorder, got {activityConsumers.Count}.");
        }

        var starter = provider.GetRequiredService<ITransponderRoutingSlipStarter>();
        var store = provider.GetRequiredService<ITransponderSagaStore>();

        lock (_Log_Lock)
            _Event_Log.Clear();
        var slipId = await starter.StartAsync(
            [
                new TransponderRoutingSlipActivityRef { Name = nameof(RoutingSlipEventOkA) },
                new TransponderRoutingSlipActivityRef { Name = nameof(RoutingSlipEventOkB) },
            ]);

        Assert.Null(await store.GetAsync(TransponderRoutingSlipPersistenceKind.SagaKind, slipId));

        List<string> snapshot;
        lock (_Log_Lock)
            snapshot = [.._Event_Log];

        Assert.Equal(
            new[] { nameof(TransponderRoutingSlipActivityCompleted) + ":0", nameof(TransponderRoutingSlipActivityCompleted) + ":1", nameof(TransponderRoutingSlipCompleted) },
            snapshot);
    }

    [Fact]
    public async Task Fault_After_First_Step_Compensates_Then_Emits_Fault_Events_Async()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTransponder(b =>
        {
            b.AddRoutingSlipActivity<RoutingSlipEventCompensatableA>();
            b.AddRoutingSlipActivity<RoutingSlipEventFaultB>();
            b.AddConsumer<TransponderRoutingSlipActivityCompleted, ActivityCompletedRecorder>();
            b.AddConsumer<TransponderRoutingSlipActivityFaulted, ActivityFaultedRecorder>();
            b.AddConsumer<TransponderRoutingSlipActivityCompensated, ActivityCompensatedRecorder>();
            b.AddConsumer<TransponderRoutingSlipFaulted, SlipFaultedRecorder>();
        });

        await using var provider = services.BuildServiceProvider();
        var starter = provider.GetRequiredService<ITransponderRoutingSlipStarter>();

        RoutingSlipEventCompensatableA.Compensated = false;
        lock (_Log_Lock)
            _Event_Log.Clear();
        await starter.StartAsync(
            [
                new TransponderRoutingSlipActivityRef { Name = nameof(RoutingSlipEventCompensatableA) },
                new TransponderRoutingSlipActivityRef { Name = nameof(RoutingSlipEventFaultB) },
            ]);

        Assert.True(RoutingSlipEventCompensatableA.Compensated);
        List<string> snapshot;
        lock (_Log_Lock)
            snapshot = [.._Event_Log];

        Assert.Equal(
            new[]
            {
                nameof(TransponderRoutingSlipActivityCompleted) + ":0",
                nameof(TransponderRoutingSlipActivityFaulted),
                nameof(TransponderRoutingSlipActivityCompensated),
                nameof(TransponderRoutingSlipFaulted),
            },
            snapshot);
    }

    [Fact]
    public async Task Compensation_Failure_Emits_Slip_Compensation_Failed_Async()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTransponder(b =>
        {
            b.AddRoutingSlipActivity<RoutingSlipEventCompensatableA>();
            b.AddRoutingSlipActivity<RoutingSlipEventCompensationFails>();
            b.AddRoutingSlipActivity<RoutingSlipEventFaultB>();
            b.AddConsumer<TransponderRoutingSlipActivityCompleted, ActivityCompletedRecorder>();
            b.AddConsumer<TransponderRoutingSlipActivityFaulted, ActivityFaultedRecorder>();
            b.AddConsumer<TransponderRoutingSlipActivityCompensated, ActivityCompensatedRecorder>();
            b.AddConsumer<TransponderRoutingSlipActivityCompensationFailed, ActivityCompensationFailedRecorder>();
            b.AddConsumer<TransponderRoutingSlipCompensationFailed, SlipCompensationFailedRecorder>();
            b.AddConsumer<TransponderRoutingSlipFaulted, SlipFaultedRecorder>();
        });

        await using var provider = services.BuildServiceProvider();
        var starter = provider.GetRequiredService<ITransponderRoutingSlipStarter>();

        RoutingSlipEventCompensatableA.Compensated = false;
        lock (_Log_Lock)
            _Event_Log.Clear();
        await starter.StartAsync(
            [
                new TransponderRoutingSlipActivityRef { Name = nameof(RoutingSlipEventCompensatableA) },
                new TransponderRoutingSlipActivityRef { Name = nameof(RoutingSlipEventCompensationFails) },
                new TransponderRoutingSlipActivityRef { Name = nameof(RoutingSlipEventFaultB) },
            ]);

        List<string> snapshot;
        lock (_Log_Lock)
            snapshot = [.._Event_Log];

        Assert.Contains(nameof(TransponderRoutingSlipActivityCompensationFailed), snapshot);
        Assert.Contains(nameof(TransponderRoutingSlipCompensationFailed), snapshot);
        Assert.Contains(nameof(TransponderRoutingSlipFaulted), snapshot);
    }

    private sealed class ActivityCompletedRecorder : IConsumer<TransponderRoutingSlipActivityCompleted>
    {
        public Task HandleAsync(ConsumeContext<TransponderRoutingSlipActivityCompleted> context)
        {
            lock (_Log_Lock)
                _Event_Log.Add(nameof(TransponderRoutingSlipActivityCompleted) + ":" + context.Message.ActivityIndex);
            return Task.CompletedTask;
        }
    }

    private sealed class SlipCompletedRecorder : IConsumer<TransponderRoutingSlipCompleted>
    {
        public Task HandleAsync(ConsumeContext<TransponderRoutingSlipCompleted> context)
        {
            lock (_Log_Lock)
                _Event_Log.Add(nameof(TransponderRoutingSlipCompleted));
            return Task.CompletedTask;
        }
    }

    private sealed class ActivityFaultedRecorder : IConsumer<TransponderRoutingSlipActivityFaulted>
    {
        public Task HandleAsync(ConsumeContext<TransponderRoutingSlipActivityFaulted> context)
        {
            lock (_Log_Lock)
                _Event_Log.Add(nameof(TransponderRoutingSlipActivityFaulted));
            return Task.CompletedTask;
        }
    }

    private sealed class ActivityCompensatedRecorder : IConsumer<TransponderRoutingSlipActivityCompensated>
    {
        public Task HandleAsync(ConsumeContext<TransponderRoutingSlipActivityCompensated> context)
        {
            lock (_Log_Lock)
                _Event_Log.Add(nameof(TransponderRoutingSlipActivityCompensated));
            return Task.CompletedTask;
        }
    }

    private sealed class ActivityCompensationFailedRecorder : IConsumer<TransponderRoutingSlipActivityCompensationFailed>
    {
        public Task HandleAsync(ConsumeContext<TransponderRoutingSlipActivityCompensationFailed> context)
        {
            lock (_Log_Lock)
                _Event_Log.Add(nameof(TransponderRoutingSlipActivityCompensationFailed));
            return Task.CompletedTask;
        }
    }

    private sealed class SlipCompensationFailedRecorder : IConsumer<TransponderRoutingSlipCompensationFailed>
    {
        public Task HandleAsync(ConsumeContext<TransponderRoutingSlipCompensationFailed> context)
        {
            lock (_Log_Lock)
                _Event_Log.Add(nameof(TransponderRoutingSlipCompensationFailed));
            return Task.CompletedTask;
        }
    }

    private sealed class SlipFaultedRecorder : IConsumer<TransponderRoutingSlipFaulted>
    {
        public Task HandleAsync(ConsumeContext<TransponderRoutingSlipFaulted> context)
        {
            lock (_Log_Lock)
                _Event_Log.Add(nameof(TransponderRoutingSlipFaulted));
            return Task.CompletedTask;
        }
    }

    private sealed class RoutingSlipEventOkA : IRoutingSlipActivity
    {
        public string Name => nameof(RoutingSlipEventOkA);

        public Task ExecuteAsync(IRoutingSlipActivityContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class RoutingSlipEventOkB : IRoutingSlipActivity
    {
        public string Name => nameof(RoutingSlipEventOkB);

        public Task ExecuteAsync(IRoutingSlipActivityContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class RoutingSlipEventCompensatableA : IRoutingSlipCompensatableActivity
    {
        public static bool Compensated;

        public string Name => nameof(RoutingSlipEventCompensatableA);

        public Task ExecuteAsync(IRoutingSlipActivityContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task CompensateAsync(IRoutingSlipActivityCompensationContext context, CancellationToken cancellationToken = default)
        {
            Compensated = true;
            return Task.CompletedTask;
        }
    }

    private sealed class RoutingSlipEventCompensationFails : IRoutingSlipCompensatableActivity
    {
        public string Name => nameof(RoutingSlipEventCompensationFails);

        public Task ExecuteAsync(IRoutingSlipActivityContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task CompensateAsync(IRoutingSlipActivityCompensationContext context, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("compensation fault");
    }

    private sealed class RoutingSlipEventFaultB : IRoutingSlipActivity
    {
        public string Name => nameof(RoutingSlipEventFaultB);

        public Task ExecuteAsync(IRoutingSlipActivityContext context, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("fault");
    }
}
