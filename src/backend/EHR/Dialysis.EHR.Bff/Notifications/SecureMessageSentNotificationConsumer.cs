using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.Module.Bff.Events;

namespace Dialysis.EHR.Bff.Notifications;

/// <summary>
/// When a patient sends a secure message, every EHR chart currently watching that patient gets a
/// "new patient message" toast so the care team can respond. Payload is metadata only — the SPA
/// refetches the thread through the synchronous, permission-checked API.
/// </summary>
public sealed class SecureMessageSentNotificationConsumer : IConsumer<PatientPortalSecureMessageSentIntegrationEvent>
{
    private readonly IBffNotifier _notifier;
    public SecureMessageSentNotificationConsumer(IBffNotifier notifier) => _notifier = notifier;

    /// <inheritdoc />
    public Task HandleAsync(ConsumeContext<PatientPortalSecureMessageSentIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var message = context.Message;
        var patientId = message.PatientId.ToString();

        var notification = new BffNotification
        {
            Type = "secure-message",
            Title = "New patient message",
            Summary = message.Subject,
            PatientId = patientId,
            Link = $"/ehr/patients/{patientId}",
            OccurredOn = new DateTimeOffset(DateTime.SpecifyKind(message.OccurredOn, DateTimeKind.Utc)),
        };

        return _notifier.PushToPatientAsync(patientId, notification, context.CancellationToken);
    }
}
