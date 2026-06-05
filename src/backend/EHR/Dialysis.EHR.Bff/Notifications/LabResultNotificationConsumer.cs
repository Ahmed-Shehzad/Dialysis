using Dialysis.BuildingBlocks.Transponder;
using Dialysis.Lab.Contracts.IntegrationEvents;
using Dialysis.Module.Bff.Events;

namespace Dialysis.EHR.Bff.Notifications;

/// <summary>
/// Bridges the Lab-owned <see cref="LabResultReceivedIntegrationEvent"/> to a live SPA push: when a
/// result is filed for a patient, every EHR chart currently watching that patient gets a "new lab
/// result" toast. The payload is metadata only — the SPA refetches the labs through the BFF's
/// synchronous, permission-checked API.
/// </summary>
public sealed class LabResultNotificationConsumer : IConsumer<LabResultReceivedIntegrationEvent>
{
    private readonly IBffNotifier _notifier;

    /// <summary>Creates the consumer over the SPA notifier.</summary>
    public LabResultNotificationConsumer(IBffNotifier notifier) => _notifier = notifier;

    /// <inheritdoc />
    public Task HandleAsync(ConsumeContext<LabResultReceivedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var message = context.Message;
        var patientId = message.PatientId.ToString();
        var observationCount = message.Observations?.Count ?? 0;

        var notification = new BffNotification
        {
            Type = "lab-result",
            Title = "New lab result",
            Summary = $"{observationCount} observation(s) for order {message.PlacerOrderNumber}",
            PatientId = patientId,
            Link = $"/ehr/patients/{patientId}/labs",
            OccurredOn = new DateTimeOffset(DateTime.SpecifyKind(message.ResultedAtUtc, DateTimeKind.Utc)),
        };

        return _notifier.PushToPatientAsync(patientId, notification, context.CancellationToken);
    }
}
