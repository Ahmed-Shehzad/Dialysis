using Dialysis.Alarm.Application.Domain;

namespace Dialysis.Alarm.Application.Abstractions;

/// <summary>
/// Adds escalation incidents for persistence. Used by domain event handlers when escalation is triggered.
/// </summary>
public interface IEscalationIncidentStore
{
    void Add(EscalationIncident incident);
}
