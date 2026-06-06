using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.Module.Bff.Events;

namespace Dialysis.PatientPortal.Bff.Notifications;

/// <summary>
/// When the care team replies to a patient on a secure-message thread, the patient's portal session
/// gets a "your care team replied" toast. Payload is metadata only — the SPA refetches the thread
/// through the synchronous, permission-checked API.
/// </summary>
public sealed class SecureMessageReceivedNotificationConsumer
    : IConsumer<PatientPortalSecureMessageReceivedIntegrationEvent>
{
    private readonly IBffNotifier _notifier;
    public SecureMessageReceivedNotificationConsumer(IBffNotifier notifier) => _notifier = notifier;

    /// <inheritdoc />
    public Task HandleAsync(ConsumeContext<PatientPortalSecureMessageReceivedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var message = context.Message;
        var patientId = message.PatientId.ToString();

        var notification = new BffNotification
        {
            Type = "secure-message",
            Title = "Your care team replied",
            Summary = message.Subject,
            PatientId = patientId,
            Link = "/portal/",
            OccurredOn = new DateTimeOffset(DateTime.SpecifyKind(message.OccurredOn, DateTimeKind.Utc)),
        };

        return _notifier.PushToPatientAsync(patientId, notification, context.CancellationToken);
    }
}
