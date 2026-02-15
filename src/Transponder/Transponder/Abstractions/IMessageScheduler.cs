namespace Transponder.Abstractions;

/// <summary>
/// Defines scheduling for sending and publishing messages in the future.
/// </summary>
public interface IMessageScheduler
{
    /// <summary>
    /// Schedules a message to be sent to a specific destination at a future time.
    /// </summary>
    /// <typeparam name="TMessage">The message type to send.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message instance to send.</param>
    /// <param name="scheduledTime">The time to send the message.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task<IScheduledMessageHandle> ScheduleSendAsync<TMessage>(
        Uri destinationAddress,
        TMessage message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken = default)
        where TMessage : class, IMessage;

    /// <summary>
    /// Schedules a message to be published at a future time.
    /// </summary>
    /// <typeparam name="TMessage">The message type to publish.</typeparam>
    /// <param name="message">The message instance to publish.</param>
    /// <param name="scheduledTime">The time to publish the message.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task<IScheduledMessageHandle> SchedulePublishAsync<TMessage>(
        TMessage message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken = default)
        where TMessage : class, IMessage;

    /// <summary>
    /// Schedules a message to be sent to a destination after a delay.
    /// </summary>
    /// <typeparam name="TMessage">The message type to send.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message instance to send.</param>
    /// <param name="delay">The delay before sending the message.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task<IScheduledMessageHandle> ScheduleSendAsync<TMessage>(
        Uri destinationAddress,
        TMessage message,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
        where TMessage : class, IMessage;

    /// <summary>
    /// Schedules a message to be published after a delay.
    /// </summary>
    /// <typeparam name="TMessage">The message type to publish.</typeparam>
    /// <param name="message">The message instance to publish.</param>
    /// <param name="delay">The delay before publishing the message.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task<IScheduledMessageHandle> SchedulePublishAsync<TMessage>(
        TMessage message,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
        where TMessage : class, IMessage;
}
