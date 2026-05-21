using Dialysis.CQRS.Queries;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.ListActiveAlarms;

/// <summary>
/// Wire shape for one machine alarm. State strings are lower-cased so the SPA's
/// existing union (<c>"present" | "inactivating" | "resolved"</c>) is the source of truth.
/// </summary>
public sealed record ActiveAlarmDto(
    Guid Id,
    Guid? SessionId,
    Guid MachineId,
    long AlarmCode,
    string? AlarmSource,
    string? AlarmPhase,
    string State,
    DateTime FirstObservedUtc,
    DateTime LastObservedUtc,
    DateTime? AcknowledgedUtc,
    string? AcknowledgedBy);

public sealed record ListActiveAlarmsQuery()
    : IQuery<IReadOnlyList<ActiveAlarmDto>>, IPermissionedCommand
{
    public string RequiredPermission => PdmsPermissions.AlarmRead;
}
