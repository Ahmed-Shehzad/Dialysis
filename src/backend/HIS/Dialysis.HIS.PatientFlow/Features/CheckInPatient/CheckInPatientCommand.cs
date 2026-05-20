using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.PatientFlow.Features.CheckInPatient;

/// <summary>
/// Moves a Queue entry from Expected to Waiting.
/// <c>ArrivalTimeUtc</c> records when the patient walked in (defaults to "now" client-side);
/// <c>EligibilityAcknowledged</c> is the receptionist's explicit confirmation that insurance
/// was checked at the counter — only meaningful for entries not pre-verified.
/// </summary>
public sealed record CheckInPatientCommand(
    Guid EntryId,
    DateTime ArrivalTimeUtc,
    bool EligibilityAcknowledged)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.PatientFlowQueueManage;
}
