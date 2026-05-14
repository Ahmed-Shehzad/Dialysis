using Dialysis.HIS.Scheduling.Domain;
using Dialysis.HIS.Scheduling.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfAppointmentRepository(HisDbContext db) : IAppointmentRepository
{
    public void Add(Appointment appointment) => db.Appointments.Add(appointment);

    public Task<Appointment?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => db.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
}
