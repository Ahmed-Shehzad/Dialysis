using Dialysis.BuildingBlocks.Transponder;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;
using Dialysis.Module.Bff.Events;

namespace Dialysis.HIS.Bff.Notifications;

/// <summary>
/// Pushes a live "patient admitted" signal to the HIS today-board: when a patient is admitted to a
/// ward, connections watching that patient (and the staff user's session) get a toast/badge. The
/// SPA refetches the board through the synchronous API.
/// </summary>
public sealed class PatientAdmittedNotificationConsumer : IConsumer<PatientAdmittedIntegrationEvent>
{
    private readonly IBffNotifier _notifier;

    /// <summary>Creates the consumer over the SPA notifier.</summary>
    public PatientAdmittedNotificationConsumer(IBffNotifier notifier) => _notifier = notifier;

    /// <inheritdoc />
    public Task HandleAsync(ConsumeContext<PatientAdmittedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var message = context.Message;
        var patientId = message.PatientId.ToString();

        var notification = new BffNotification
        {
            Type = "patient-admitted",
            Title = "Patient admitted",
            Summary = $"Admitted to ward {message.WardCode}",
            PatientId = patientId,
            Link = $"/his/patients/{patientId}",
            OccurredOn = new DateTimeOffset(DateTime.SpecifyKind(message.AdmittedAtUtc, DateTimeKind.Utc)),
        };

        return _notifier.PushToPatientAsync(patientId, notification, context.CancellationToken);
    }
}
