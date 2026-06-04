using Dialysis.HIS.Scheduling.Domain;
using Dialysis.HIS.Scheduling.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfAppointmentRepository : IAppointmentRepository
{
    private readonly HisDbContext _db;
    public EfAppointmentRepository(HisDbContext db) => _db = db;
    public void Add(Appointment appointment) => _db.Appointments.Add(appointment);

    public Task<Appointment?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
}
