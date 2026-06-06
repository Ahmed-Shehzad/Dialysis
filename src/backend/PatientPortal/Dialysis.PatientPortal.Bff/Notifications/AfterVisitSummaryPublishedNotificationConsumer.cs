using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.Module.Bff.Events;

namespace Dialysis.PatientPortal.Bff.Notifications;

/// <summary>
/// When a clinician publishes an after-visit summary, toast the patient's portal session so they can
/// read their visit instructions. Metadata only — the SPA refetches via the synchronous API.
/// </summary>
public sealed class AfterVisitSummaryPublishedNotificationConsumer
    : IConsumer<AfterVisitSummaryPublishedIntegrationEvent>
{
    private readonly IBffNotifier _notifier;
    public AfterVisitSummaryPublishedNotificationConsumer(IBffNotifier notifier) => _notifier = notifier;

    /// <inheritdoc />
    public Task HandleAsync(ConsumeContext<AfterVisitSummaryPublishedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var message = context.Message;
        var patientId = message.PatientId.ToString();

        var notification = new BffNotification
        {
            Type = "after-visit-summary",
            Title = "Your visit summary is ready",
            Summary = "Instructions and follow-up from your recent visit.",
            PatientId = patientId,
            Link = "/portal/",
            OccurredOn = new DateTimeOffset(DateTime.SpecifyKind(message.OccurredOn, DateTimeKind.Utc)),
        };

        return _notifier.PushToPatientAsync(patientId, notification, context.CancellationToken);
    }
}
