using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.Module.Bff.Events;

namespace Dialysis.EHR.Bff.Notifications;

/// <summary>
/// When a patient requests an appointment, surface a "new appointment request" toast to staff viewing
/// that patient. Metadata only — staff work the request from the synchronous worklist.
/// </summary>
public sealed class AppointmentRequestedNotificationConsumer
    : IConsumer<PatientPortalAppointmentRequestedIntegrationEvent>
{
    private readonly IBffNotifier _notifier;
    public AppointmentRequestedNotificationConsumer(IBffNotifier notifier) => _notifier = notifier;

    /// <inheritdoc />
    public Task HandleAsync(ConsumeContext<PatientPortalAppointmentRequestedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var message = context.Message;
        var patientId = message.PatientId.ToString();

        var notification = new BffNotification
        {
            Type = "appointment-request",
            Title = "New appointment request",
            Summary = message.ReasonText,
            PatientId = patientId,
            Link = "/appointment-requests",
            OccurredOn = new DateTimeOffset(DateTime.SpecifyKind(message.OccurredOn, DateTimeKind.Utc)),
        };

        return _notifier.PushToPatientAsync(patientId, notification, context.CancellationToken);
    }
}
