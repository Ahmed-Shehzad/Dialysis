using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Application.Domain;

namespace Dialysis.Alarm.Infrastructure.Persistence;

public sealed class EscalationIncidentStore : IEscalationIncidentStore
{
    private readonly AlarmDbContext _db;

    public EscalationIncidentStore(AlarmDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public void Add(EscalationIncident incident)
    {
        _ = _db.EscalationIncidents.Add(incident);
    }
}
