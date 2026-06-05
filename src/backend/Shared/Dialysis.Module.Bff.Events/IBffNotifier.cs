namespace Dialysis.Module.Bff.Events;

/// <summary>
/// Pushes a <see cref="BffNotification"/> to the context SPA over the SignalR notifications hub.
/// Integration-event consumers in a BFF map their event to a notification and call this; the SPA
/// then refetches through the synchronous API. Scoped pushes target the patient/user groups the
/// SPA joins on connect.
/// </summary>
public interface IBffNotifier
{
    /// <summary>Pushes to every connection currently watching <paramref name="patientId"/>.</summary>
    Task PushToPatientAsync(string patientId, BffNotification notification, CancellationToken cancellationToken = default);

    /// <summary>Pushes to every connection authenticated as <paramref name="userSubject"/> (the OIDC <c>sub</c>).</summary>
    Task PushToUserAsync(string userSubject, BffNotification notification, CancellationToken cancellationToken = default);
}
