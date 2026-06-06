using Dialysis.BuildingBlocks.Transponder;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;
using Dialysis.Module.Bff.Events;

namespace Dialysis.EHR.Bff.Notifications;

/// <summary>
/// Pushes a live SPA toast when a patient is admitted in HIS, so any EHR chart watching that patient
/// learns of the hospital stay. Metadata only — the SPA refetches the follow-up worklist / chart card
/// through the synchronous, permission-checked API.
/// </summary>
public sealed class HospitalAdmitNotificationConsumer : IConsumer<PatientAdmittedIntegrationEvent>
{
    private readonly IBffNotifier _notifier;
    public HospitalAdmitNotificationConsumer(IBffNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(ConsumeContext<PatientAdmittedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var m = context.Message;
        var patientId = m.PatientId.ToString();
        return _notifier.PushToPatientAsync(patientId, new BffNotification
        {
            Type = "hospital-event",
            Title = "Patient admitted",
            Summary = $"Admitted to ward {m.WardCode}",
            PatientId = patientId,
            Link = $"/ehr/patients/{patientId}",
            OccurredOn = new DateTimeOffset(DateTime.SpecifyKind(m.AdmittedAtUtc, DateTimeKind.Utc)),
        }, context.CancellationToken);
    }
}

/// <summary>Pushes a live SPA toast when a patient is discharged — the proactive-follow-up prompt.</summary>
public sealed class HospitalDischargeNotificationConsumer : IConsumer<PatientDischargedIntegrationEvent>
{
    private readonly IBffNotifier _notifier;
    public HospitalDischargeNotificationConsumer(IBffNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(ConsumeContext<PatientDischargedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var m = context.Message;
        var patientId = m.PatientId.ToString();
        return _notifier.PushToPatientAsync(patientId, new BffNotification
        {
            Type = "hospital-event",
            Title = "Patient discharged",
            Summary = $"Discharged from ward {m.WardCode} — follow up",
            PatientId = patientId,
            Link = $"/ehr/patients/{patientId}",
            OccurredOn = new DateTimeOffset(DateTime.SpecifyKind(m.DischargedAtUtc, DateTimeKind.Utc)),
        }, context.CancellationToken);
    }
}
