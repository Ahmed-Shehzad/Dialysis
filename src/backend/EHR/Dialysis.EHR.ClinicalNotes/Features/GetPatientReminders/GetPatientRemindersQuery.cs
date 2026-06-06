using Dialysis.CQRS.Queries;
using Dialysis.EHR.ClinicalNotes.Features.QualityMeasures;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.GetPatientReminders;

/// <summary>A plain-language health reminder shown to the patient in the portal.</summary>
public sealed record PatientReminderDto(string Title, string WhatToDo, string? ResourceUrl);

/// <summary>
/// The patient-facing view of their open quality-measure gaps, mapped to plain-language reminders.
/// Runs the same <see cref="IQualityMeasureEvaluator"/> the clinician chart uses — empty unless
/// measures are configured.
/// </summary>
public sealed record GetPatientRemindersQuery(Guid PatientId)
    : IQuery<IReadOnlyList<PatientReminderDto>>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.PortalRemindersRead;
}

public sealed class GetPatientRemindersQueryHandler
    : IQueryHandler<GetPatientRemindersQuery, IReadOnlyList<PatientReminderDto>>
{
    private readonly IQualityMeasureEvaluator _evaluator;
    public GetPatientRemindersQueryHandler(IQualityMeasureEvaluator evaluator) => _evaluator = evaluator;

    public async Task<IReadOnlyList<PatientReminderDto>> HandleAsync(
        GetPatientRemindersQuery request, CancellationToken cancellationToken)
    {
        var gaps = await _evaluator.EvaluateAsync(request.PatientId, cancellationToken).ConfigureAwait(false);
        return [.. gaps.Select(ToReminder)];
    }

    // The evaluator's Detail is clinician-oriented (LOINC codes, lookback windows); the reminder keeps
    // the measure Title and gives the patient a concrete, non-clinical next step.
    private static PatientReminderDto ToReminder(QualityGap gap) =>
        new(gap.Title, "Talk to your care team about scheduling this.", ResourceUrl: null);
}
