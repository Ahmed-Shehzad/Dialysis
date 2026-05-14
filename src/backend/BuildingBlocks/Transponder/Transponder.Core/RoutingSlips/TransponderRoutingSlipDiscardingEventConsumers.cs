using Dialysis.BuildingBlocks.Transponder.RoutingSlips.Events;

namespace Dialysis.BuildingBlocks.Transponder.RoutingSlips;

/// <summary>
/// Default no-op consumers so routing slip events always have an in-process consume route (additional observers can be registered with <c>AddConsumer&lt;TEvent, T&gt;()</c>).
/// </summary>
internal static class TransponderRoutingSlipDiscardingEventConsumers
{
    public static void Register(TransponderBuilder builder)
    {
        builder.AddConsumer<TransponderRoutingSlipActivityCompleted, DiscardingActivityCompleted>();
        builder.AddConsumer<TransponderRoutingSlipActivityFaulted, DiscardingActivityFaulted>();
        builder.AddConsumer<TransponderRoutingSlipActivityCompensated, DiscardingActivityCompensated>();
        builder.AddConsumer<TransponderRoutingSlipActivityCompensationFailed, DiscardingActivityCompensationFailed>();
        builder.AddConsumer<TransponderRoutingSlipCompensationFailed, DiscardingSlipCompensationFailed>();
        builder.AddConsumer<TransponderRoutingSlipCompleted, DiscardingSlipCompleted>();
        builder.AddConsumer<TransponderRoutingSlipFaulted, DiscardingSlipFaulted>();
    }

    private sealed class DiscardingActivityCompleted : IConsumer<TransponderRoutingSlipActivityCompleted>
    {
        public Task HandleAsync(ConsumeContext<TransponderRoutingSlipActivityCompleted> context) => Task.CompletedTask;
    }

    private sealed class DiscardingActivityFaulted : IConsumer<TransponderRoutingSlipActivityFaulted>
    {
        public Task HandleAsync(ConsumeContext<TransponderRoutingSlipActivityFaulted> context) => Task.CompletedTask;
    }

    private sealed class DiscardingActivityCompensated : IConsumer<TransponderRoutingSlipActivityCompensated>
    {
        public Task HandleAsync(ConsumeContext<TransponderRoutingSlipActivityCompensated> context) => Task.CompletedTask;
    }

    private sealed class DiscardingActivityCompensationFailed : IConsumer<TransponderRoutingSlipActivityCompensationFailed>
    {
        public Task HandleAsync(ConsumeContext<TransponderRoutingSlipActivityCompensationFailed> context) => Task.CompletedTask;
    }

    private sealed class DiscardingSlipCompensationFailed : IConsumer<TransponderRoutingSlipCompensationFailed>
    {
        public Task HandleAsync(ConsumeContext<TransponderRoutingSlipCompensationFailed> context) => Task.CompletedTask;
    }

    private sealed class DiscardingSlipCompleted : IConsumer<TransponderRoutingSlipCompleted>
    {
        public Task HandleAsync(ConsumeContext<TransponderRoutingSlipCompleted> context) => Task.CompletedTask;
    }

    private sealed class DiscardingSlipFaulted : IConsumer<TransponderRoutingSlipFaulted>
    {
        public Task HandleAsync(ConsumeContext<TransponderRoutingSlipFaulted> context) => Task.CompletedTask;
    }
}
