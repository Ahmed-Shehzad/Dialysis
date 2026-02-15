namespace Intercessor.Abstractions;

/// <summary>
/// Defines a handler for a specific type of notification.
/// Implement this interface to handle notifications that implement <see cref="INotification"/>.
/// </summary>
/// <typeparam name="TNotification">
/// The type of notification to handle. Must implement <see cref="INotification"/>.
/// </typeparam>
public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    /// <summary>
    /// Handles the notification asynchronously.
    /// </summary>
    /// <param name="notification">The notification instance to handle.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken = default);
}

public interface INotificationHandler<TResponse, in TNotification> where TNotification : INotification
{
    Task<TResponse?> HandleAsync(TNotification notification, CancellationToken cancellationToken = default);
}
