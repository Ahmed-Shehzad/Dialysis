using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.Module.Bff.Events;

namespace Dialysis.PatientPortal.Bff.Notifications;

/// <summary>
/// When staff approve or decline a patient's appointment request, toast the patient's portal session.
/// Metadata only — the SPA refetches the request list through the synchronous, permission-checked API.
/// </summary>
public sealed class AppointmentResolvedNotificationConsumer
    : IConsumer<PatientPortalAppointmentResolvedIntegrationEvent>
{
    private readonly IBffNotifier _notifier;
    public AppointmentResolvedNotificationConsumer(IBffNotifier notifier) => _notifier = notifier;

    /// <inheritdoc />
    public Task HandleAsync(ConsumeContext<PatientPortalAppointmentResolvedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var message = context.Message;
        var patientId = message.PatientId.ToString();

        var notification = new BffNotification
        {
            Type = "appointment-request",
            Title = message.Approved ? "Appointment request approved" : "Appointment request declined",
            Summary = message.StaffNote,
            PatientId = patientId,
            Link = "/portal/",
            OccurredOn = new DateTimeOffset(DateTime.SpecifyKind(message.OccurredOn, DateTimeKind.Utc)),
        };

        return _notifier.PushToPatientAsync(patientId, notification, context.CancellationToken);
    }
}
