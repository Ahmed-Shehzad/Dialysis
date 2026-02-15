namespace Intercessor.Abstractions;

/// <summary>
/// Defines a publisher responsible for broadcasting notifications to all registered <see cref="INotificationHandler{TNotification}"/> instances.
/// </summary>
public interface IPublisher
{
    /// <summary>
    /// Publishes a notification to all registered handlers asynchronously (One-to-Many).
    /// </summary>
    /// <typeparam name="TNotification">The type of notification being published. Must implement <see cref="INotification"/>.</typeparam>
    /// <param name="notification">The notification instance to publish.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous publish operation.</returns>
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification;
}
