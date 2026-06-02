using Dialysis.PDMS.OnCall.Domain;

namespace Dialysis.PDMS.OnCall.Dispatch;

/// <summary>Looks up the active rotation covering a chair at a given instant.</summary>
public interface IOnCallRotationLookup
{
    Task<OnCallRotation?> FindActiveAsync(Guid chairId, DateTime atUtc, CancellationToken cancellationToken);
}

/// <summary>Looks up the active escalation policy. Single policy per facility for now.</summary>
public interface IEscalationPolicyLookup
{
    Task<EscalationPolicy?> FindActiveAsync(CancellationToken cancellationToken);
}

/// <summary>Persists the <see cref="AlarmDispatch"/> audit aggregate.</summary>
public interface IAlarmDispatchRepository
{
    Task AddAsync(AlarmDispatch dispatch, CancellationToken cancellationToken);
    Task<AlarmDispatch?> FindAsync(Guid dispatchId, CancellationToken cancellationToken);
}
