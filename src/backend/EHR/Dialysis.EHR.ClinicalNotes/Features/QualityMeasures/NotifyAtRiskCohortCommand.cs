using Dialysis.BuildingBlocks.ClinicianNotification;
using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;
using Microsoft.Extensions.Options;

namespace Dialysis.EHR.ClinicalNotes.Features.QualityMeasures;

/// <summary>One patient targeted for outreach (clinician-facing; carries the MRN for the worklist).</summary>
public sealed record OutreachTarget(Guid PatientId, string MedicalRecordNumber, string Name, bool ContactResolved);

/// <summary>Audited result of an at-risk outreach run.</summary>
public sealed record OutreachResult(string MeasureId, int Targeted, bool Dispatched, IReadOnlyList<OutreachTarget> Targets);

/// <summary>
/// Resolves the patients whose condition is uncontrolled for a measure and reaches out to them. v1 is
/// honest about the missing per-patient contact store: it always returns the audited target list, and
/// only calls the (PHI-minimised) <see cref="IClinicianNotificationDispatcher"/> when outreach is
/// enabled in config and a contact resolves.
/// </summary>
public sealed record NotifyAtRiskCohortCommand(string MeasureId, int Take = 100)
    : ICommand<OutreachResult>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.PopulationOutreach;
}

public sealed class NotifyAtRiskCohortCommandHandler : ICommandHandler<NotifyAtRiskCohortCommand, OutreachResult>
{
    private readonly ICqrsGateway _gateway;
    private readonly IOutreachContactResolver _contacts;
    private readonly IClinicianNotificationDispatcher _dispatcher;
    private readonly OutreachOptions _options;

    public NotifyAtRiskCohortCommandHandler(
        ICqrsGateway gateway,
        IOutreachContactResolver contacts,
        IClinicianNotificationDispatcher dispatcher,
        IOptions<OutreachOptions> options)
    {
        _gateway = gateway;
        _contacts = contacts;
        _dispatcher = dispatcher;
        _options = options.Value;
    }

    public async Task<OutreachResult> HandleAsync(NotifyAtRiskCohortCommand request, CancellationToken cancellationToken)
    {
        var control = await _gateway.SendQueryAsync<EvaluatePopulationControlQuery, PopulationControlResult>(
            new EvaluatePopulationControlQuery(request.MeasureId, request.Take), cancellationToken).ConfigureAwait(false);

        var atRisk = control.Breakdown
            .Where(b => b.Outcome == nameof(PatientControlOutcome.Uncontrolled))
            .ToList();

        var targets = new List<OutreachTarget>(atRisk.Count);
        var requests = new List<ClinicianNotificationRequest>();
        foreach (var patient in atRisk)
        {
            var contact = _contacts.Resolve(patient.PatientId);
            targets.Add(new OutreachTarget(patient.PatientId, patient.MedicalRecordNumber, patient.Name, contact is not null));
            if (contact is not null)
            {
                // PHI-minimised: no name / MRN / diagnosis in the body; the patient id rides in metadata.
                requests.Add(new ClinicianNotificationRequest(
                    Channel: contact.Channel,
                    Address: contact.Address,
                    Subject: "A care reminder is available",
                    Body: "Your care team would like to follow up. Please contact the clinic or check your portal.",
                    DeepLink: "/portal/",
                    Priority: NotificationPriority.Normal,
                    Metadata: new Dictionary<string, string> { ["patientId"] = patient.PatientId.ToString(), ["measureId"] = control.MeasureId }));
            }
        }

        var dispatched = false;
        if (_options.Enabled && requests.Count > 0)
        {
            await _dispatcher.DispatchAsync(requests, cancellationToken).ConfigureAwait(false);
            dispatched = true;
        }

        return new OutreachResult(control.MeasureId, targets.Count, dispatched, targets);
    }
}
