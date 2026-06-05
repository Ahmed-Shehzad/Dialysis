using Microsoft.AspNetCore.SignalR;

namespace Dialysis.Module.Bff.Events;

/// <summary><see cref="IBffNotifier"/> over <see cref="IHubContext{THub}"/> for <see cref="NotificationsHub"/>.</summary>
public sealed class SignalRBffNotifier : IBffNotifier
{
    private readonly IHubContext<NotificationsHub> _hub;

    /// <summary>Creates the notifier over the hub context.</summary>
    public SignalRBffNotifier(IHubContext<NotificationsHub> hub) => _hub = hub;

    /// <inheritdoc />
    public Task PushToPatientAsync(string patientId, BffNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patientId);
        ArgumentNullException.ThrowIfNull(notification);
        return _hub.Clients
            .Group(NotificationsHub.PatientGroup(patientId))
            .SendAsync(NotificationsHub.EventName, notification, cancellationToken);
    }

    /// <inheritdoc />
    public Task PushToUserAsync(string userSubject, BffNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userSubject);
        ArgumentNullException.ThrowIfNull(notification);
        return _hub.Clients
            .Group(NotificationsHub.UserGroup(userSubject))
            .SendAsync(NotificationsHub.EventName, notification, cancellationToken);
    }
}
