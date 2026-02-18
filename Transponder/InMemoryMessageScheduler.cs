using Transponder.Abstractions;

namespace Transponder;

/// <summary>
/// In-memory message scheduler for delayed send/publish.
/// </summary>
public sealed class InMemoryMessageScheduler : IMessageScheduler
{
    private readonly TransponderBus _bus;

    public InMemoryMessageScheduler(TransponderBus bus)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
    }

    public Task<IScheduledMessageHandle> ScheduleSendAsync<TMessage>(
        Uri destinationAddress,
        TMessage message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
    {
        TimeSpan delay = scheduledTime - DateTimeOffset.UtcNow;
        if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;

        return ScheduleSendAsync(destinationAddress, message, delay, cancellationToken);
    }

    public Task<IScheduledMessageHandle> SchedulePublishAsync<TMessage>(
        TMessage message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
    {
        TimeSpan delay = scheduledTime - DateTimeOffset.UtcNow;
        if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;

        return SchedulePublishAsync(message, delay, cancellationToken);
    }

    public Task<IScheduledMessageHandle> ScheduleSendAsync<TMessage>(
        Uri destinationAddress,
        TMessage message,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(message);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var handle = new ScheduledMessageHandle(Ulid.NewUlid(), cts);

        _ = ExecuteAsync(
            delay,
            cts.Token,
            () => _bus.SendInternalAsync(destinationAddress, message, null, null, null, null, cts.Token));

        return Task.FromResult<IScheduledMessageHandle>(handle);
    }

    public Task<IScheduledMessageHandle> SchedulePublishAsync<TMessage>(
        TMessage message,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
    {
        ArgumentNullException.ThrowIfNull(message);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var handle = new ScheduledMessageHandle(Ulid.NewUlid(), cts);

        _ = ExecuteAsync(delay, cts.Token, () => _bus.PublishInternalAsync(message, null, cts.Token));

        return Task.FromResult<IScheduledMessageHandle>(handle);
    }

    private async static Task ExecuteAsync(
        TimeSpan delay,
        CancellationToken cancellationToken,
        Func<Task> operation)
    {
        try
        {
            if (delay > TimeSpan.Zero) await Task.Delay(delay, cancellationToken).ConfigureAwait(false);

            if (!cancellationToken.IsCancellationRequested) await operation().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }
}
