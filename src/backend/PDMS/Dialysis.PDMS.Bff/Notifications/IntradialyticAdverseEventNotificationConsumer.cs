using Dialysis.BuildingBlocks.Transponder;
using Dialysis.Module.Bff.Events;
using Dialysis.PDMS.Contracts.Integration;

namespace Dialysis.PDMS.Bff.Notifications;

/// <summary>
/// Raises a live chairside alarm: when an intradialytic adverse event is recorded mid-session, the
/// PDMS SPA watching that patient gets an immediate toast. The payload carries only the alarm kind +
/// severity (no clinical narrative); the chairside view refetches the session through the API.
/// </summary>
public sealed class IntradialyticAdverseEventNotificationConsumer : IConsumer<IntradialyticAdverseEventIntegrationEvent>
{
    private readonly IBffNotifier _notifier;

    /// <summary>Creates the consumer over the SPA notifier.</summary>
    public IntradialyticAdverseEventNotificationConsumer(IBffNotifier notifier) => _notifier = notifier;

    /// <inheritdoc />
    public Task HandleAsync(ConsumeContext<IntradialyticAdverseEventIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var message = context.Message;
        var patientId = message.PatientId.ToString();

        var notification = new BffNotification
        {
            Type = "intradialytic-adverse-event",
            Title = "Chairside alarm",
            Summary = $"{message.Severity} adverse event ({message.EventKindCode})",
            PatientId = patientId,
            Link = $"/pdms/sessions/{message.SessionId}",
            OccurredOn = new DateTimeOffset(DateTime.SpecifyKind(message.ObservedAtUtc, DateTimeKind.Utc)),
        };

        return _notifier.PushToPatientAsync(patientId, notification, context.CancellationToken);
    }
}
