using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Dialysis.BuildingBlocks.Transponder.Tests;

[Collection(nameof(RoutingSlipTestCollection))]
public sealed class RoutingSlipEventTests
{
    private static readonly object LogLock = new();

    private static readonly List<string> EventLog = [];

    [Fact]
    public async Task Happy_path_emits_activity_completed_then_slip_completed()
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

        lock (LogLock)
            EventLog.Clear();
        var slipId = await starter.StartAsync(
            [
                new TransponderRoutingSlipActivityRef { Name = nameof(RoutingSlipEventOkA) },
                new TransponderRoutingSlipActivityRef { Name = nameof(RoutingSlipEventOkB) },
            ]);

        Assert.Null(await store.GetAsync(TransponderRoutingSlipPersistenceKind.SagaKind, slipId));

        List<string> snapshot;
        lock (LogLock)
            snapshot = [..EventLog];

        Assert.Equal(
            new[] { nameof(TransponderRoutingSlipActivityCompleted) + ":0", nameof(TransponderRoutingSlipActivityCompleted) + ":1", nameof(TransponderRoutingSlipCompleted) },
            snapshot);
    }

    [Fact]
    public async Task Fault_after_first_step_compensates_then_emits_fault_events()
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
        lock (LogLock)
            EventLog.Clear();
        await starter.StartAsync(
            [
                new TransponderRoutingSlipActivityRef { Name = nameof(RoutingSlipEventCompensatableA) },
                new TransponderRoutingSlipActivityRef { Name = nameof(RoutingSlipEventFaultB) },
            ]);

        Assert.True(RoutingSlipEventCompensatableA.Compensated);
        List<string> snapshot;
        lock (LogLock)
            snapshot = [..EventLog];

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
    public async Task Compensation_failure_emits_slip_compensation_failed()
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
        lock (LogLock)
            EventLog.Clear();
        await starter.StartAsync(
            [
                new TransponderRoutingSlipActivityRef { Name = nameof(RoutingSlipEventCompensatableA) },
                new TransponderRoutingSlipActivityRef { Name = nameof(RoutingSlipEventCompensationFails) },
                new TransponderRoutingSlipActivityRef { Name = nameof(RoutingSlipEventFaultB) },
            ]);

        List<string> snapshot;
        lock (LogLock)
            snapshot = [..EventLog];

        Assert.Contains(nameof(TransponderRoutingSlipActivityCompensationFailed), snapshot);
        Assert.Contains(nameof(TransponderRoutingSlipCompensationFailed), snapshot);
        Assert.Contains(nameof(TransponderRoutingSlipFaulted), snapshot);
    }

    private sealed class ActivityCompletedRecorder : IConsumer<TransponderRoutingSlipActivityCompleted>
    {
        public Task Handle(ConsumeContext<TransponderRoutingSlipActivityCompleted> context)
        {
            lock (LogLock)
                EventLog.Add(nameof(TransponderRoutingSlipActivityCompleted) + ":" + context.Message.ActivityIndex);
            return Task.CompletedTask;
        }
    }

    private sealed class SlipCompletedRecorder : IConsumer<TransponderRoutingSlipCompleted>
    {
        public Task Handle(ConsumeContext<TransponderRoutingSlipCompleted> context)
        {
            lock (LogLock)
                EventLog.Add(nameof(TransponderRoutingSlipCompleted));
            return Task.CompletedTask;
        }
    }

    private sealed class ActivityFaultedRecorder : IConsumer<TransponderRoutingSlipActivityFaulted>
    {
        public Task Handle(ConsumeContext<TransponderRoutingSlipActivityFaulted> context)
        {
            lock (LogLock)
                EventLog.Add(nameof(TransponderRoutingSlipActivityFaulted));
            return Task.CompletedTask;
        }
    }

    private sealed class ActivityCompensatedRecorder : IConsumer<TransponderRoutingSlipActivityCompensated>
    {
        public Task Handle(ConsumeContext<TransponderRoutingSlipActivityCompensated> context)
        {
            lock (LogLock)
                EventLog.Add(nameof(TransponderRoutingSlipActivityCompensated));
            return Task.CompletedTask;
        }
    }

    private sealed class ActivityCompensationFailedRecorder : IConsumer<TransponderRoutingSlipActivityCompensationFailed>
    {
        public Task Handle(ConsumeContext<TransponderRoutingSlipActivityCompensationFailed> context)
        {
            lock (LogLock)
                EventLog.Add(nameof(TransponderRoutingSlipActivityCompensationFailed));
            return Task.CompletedTask;
        }
    }

    private sealed class SlipCompensationFailedRecorder : IConsumer<TransponderRoutingSlipCompensationFailed>
    {
        public Task Handle(ConsumeContext<TransponderRoutingSlipCompensationFailed> context)
        {
            lock (LogLock)
                EventLog.Add(nameof(TransponderRoutingSlipCompensationFailed));
            return Task.CompletedTask;
        }
    }

    private sealed class SlipFaultedRecorder : IConsumer<TransponderRoutingSlipFaulted>
    {
        public Task Handle(ConsumeContext<TransponderRoutingSlipFaulted> context)
        {
            lock (LogLock)
                EventLog.Add(nameof(TransponderRoutingSlipFaulted));
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
