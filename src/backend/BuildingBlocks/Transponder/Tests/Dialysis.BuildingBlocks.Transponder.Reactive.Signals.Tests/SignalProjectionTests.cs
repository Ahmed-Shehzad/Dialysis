using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.BuildingBlocks.Transponder.Reactive.Signals.Tests;

public class SignalProjectionTests
{
    private sealed record TreatmentStatusChanged(string SessionId, int Sequence, string Status);

    private sealed record TreatmentBoardState(int AppliedCount, string? LastStatus, int HighestSequence)
    {
        public static TreatmentBoardState Empty { get; } = new(0, null, 0);
    }

    private sealed class TreatmentBoardProjection : SignalProjection<TreatmentStatusChanged, TreatmentBoardState>
    {
        public const string Name = "treatment-board";

        public TreatmentBoardProjection(SignalStore store)
            : base(store)
        {
        }

        protected override string StateName => Name;

        protected override TreatmentBoardState CreateInitialState() => TreatmentBoardState.Empty;

        protected override TreatmentBoardState Reduce(TreatmentBoardState current, TreatmentStatusChanged message) =>
            // Monotonic guard: replayed or reordered deliveries (at-least-once bus) fold to a no-op.
            message.Sequence <= current.HighestSequence
                ? current
                : new TreatmentBoardState(current.AppliedCount + 1, message.Status, message.Sequence);
    }

    [Fact]
    public async Task Projection_Folds_Messages_Into_Singleton_State_Async()
    {
        var store = new SignalStore();
        var projection = new TreatmentBoardProjection(store);

        await projection.HandleAsync(CreateContext(new TreatmentStatusChanged("s1", 1, "Running")));
        await projection.HandleAsync(CreateContext(new TreatmentStatusChanged("s1", 2, "Completed")));

        var state = store.TryGet<TreatmentBoardState>(TreatmentBoardProjection.Name);
        Assert.NotNull(state);
        Assert.Equal(new TreatmentBoardState(2, "Completed", 2), state.Value);
    }

    [Fact]
    public async Task Scoped_Consumer_Instances_Share_One_Signal_Across_Scopes_Async()
    {
        var services = new ServiceCollection();
        services.AddTransponder(t => t.AddSignalProjection<TreatmentStatusChanged, TreatmentBoardProjection>());
        await using var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            var consumer = (TreatmentBoardProjection)scope.ServiceProvider
                .GetRequiredService<IConsumer<TreatmentStatusChanged>>();
            await consumer.HandleAsync(CreateContext(new TreatmentStatusChanged("s1", 1, "Running")));
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var consumer = (TreatmentBoardProjection)scope.ServiceProvider
                .GetRequiredService<IConsumer<TreatmentStatusChanged>>();
            await consumer.HandleAsync(CreateContext(new TreatmentStatusChanged("s1", 2, "Completed")));
        }

        var store = provider.GetRequiredService<SignalStore>();
        var state = store.TryGet<TreatmentBoardState>(TreatmentBoardProjection.Name);
        Assert.NotNull(state);
        Assert.Equal(new TreatmentBoardState(2, "Completed", 2), state.Value);
    }

    [Fact]
    public async Task Replayed_Message_With_Monotonic_Reducer_Is_Idempotent_Async()
    {
        var store = new SignalStore();
        var projection = new TreatmentBoardProjection(store);
        var message = new TreatmentStatusChanged("s1", 1, "Running");

        await projection.HandleAsync(CreateContext(message));
        await projection.HandleAsync(CreateContext(message)); // at-least-once redelivery

        var state = store.TryGet<TreatmentBoardState>(TreatmentBoardProjection.Name);
        Assert.NotNull(state);
        Assert.Equal(new TreatmentBoardState(1, "Running", 1), state.Value);
    }

    [Fact]
    public async Task Concurrent_Deliveries_Fold_Without_Lost_Updates_Async()
    {
        const int deliveries = 5_000;
        var store = new SignalStore();

        await Task.WhenAll(Enumerable.Range(1, deliveries).Select(sequence => Task.Run(async () =>
        {
            // A fresh consumer instance per delivery, like real scoped consumption.
            var projection = new TreatmentBoardProjection(store);
            await projection.HandleAsync(CreateContext(new TreatmentStatusChanged("s1", sequence, $"step-{sequence}")));
        })));

        var state = store.TryGet<TreatmentBoardState>(TreatmentBoardProjection.Name);
        Assert.NotNull(state);
        Assert.Equal(deliveries, state.Value.HighestSequence);
        // The monotonic guard may legitimately drop out-of-order arrivals; what must never
        // happen is a lost update on the applied path: the highest sequence always lands and
        // the applied count is at least 1 and at most the delivery count.
        Assert.InRange(state.Value.AppliedCount, 1, deliveries);
    }

    private static ConsumeContext<TreatmentStatusChanged> CreateContext(TreatmentStatusChanged message) =>
        new(message, CancellationToken.None, NoopBus.Instance);

    private sealed class NoopBus : ITransponderBus
    {
        public static NoopBus Instance { get; } = new();

        public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
            where TMessage : class => Task.CompletedTask;

        public Task PublishAsync<TMessage>(TMessage message, TransponderPublishOptions options, CancellationToken cancellationToken = default)
            where TMessage : class => Task.CompletedTask;

        public Task PublishPreparedAsync(string routingKey, object message, TransponderPublishOptions options, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task PublishLargeAsync<TMessage>(TMessage message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default)
            where TMessage : class => Task.CompletedTask;
    }
}
