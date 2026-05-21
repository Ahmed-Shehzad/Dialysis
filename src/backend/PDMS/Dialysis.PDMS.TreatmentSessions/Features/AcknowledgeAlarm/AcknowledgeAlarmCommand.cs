using Dialysis.CQRS.Commands;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.AcknowledgeAlarm;

/// <summary>
/// Records that a clinician has seen and accepted responsibility for a live machine alarm.
/// Aggregate-level <see cref="Domain.TreatmentAlarm.Acknowledge"/> is idempotent — the first
/// caller wins, repeated calls are a no-op — and is orthogonal to the alarm's state machine,
/// so this command is also valid after the alarm has resolved (a clinician may still wish to
/// note they reviewed it). The acknowledger string is sourced from the authenticated user by
/// the controller; the SPA does not get to choose who acknowledged.
/// </summary>
public sealed record AcknowledgeAlarmCommand(Guid AlarmId, string AcknowledgedBy)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => PdmsPermissions.AlarmAcknowledge;
}
