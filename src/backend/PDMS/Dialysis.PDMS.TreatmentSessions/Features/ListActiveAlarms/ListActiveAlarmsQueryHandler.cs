using Dialysis.CQRS.Queries;
using Dialysis.PDMS.TreatmentSessions.Domain;
using Dialysis.PDMS.TreatmentSessions.Ports;

namespace Dialysis.PDMS.TreatmentSessions.Features.ListActiveAlarms;

public sealed class ListActiveAlarmsQueryHandler(ITreatmentAlarmRepository alarms)
    : IQueryHandler<ListActiveAlarmsQuery, IReadOnlyList<ActiveAlarmDto>>
{
    public async Task<IReadOnlyList<ActiveAlarmDto>> HandleAsync(
        ListActiveAlarmsQuery _,
        CancellationToken cancellationToken)
    {
        var active = await alarms.ListActiveAsync(cancellationToken).ConfigureAwait(false);
        return [.. active.Select(a => new ActiveAlarmDto(
            a.Id,
            a.SessionId,
            a.MachineId,
            a.AlarmCode,
            a.AlarmSource,
            a.AlarmPhase,
            ToWireState(a.State),
            a.FirstObservedUtc,
            a.LastObservedUtc,
            a.AcknowledgedUtc,
            a.AcknowledgedBy))];
    }

    private static string ToWireState(TreatmentAlarmState state) => state switch
    {
        TreatmentAlarmState.Present => "present",
        TreatmentAlarmState.Inactivating => "inactivating",
        TreatmentAlarmState.Resolved => "resolved",
        _ => throw new InvalidOperationException($"Unknown alarm state {state}."),
    };
}
