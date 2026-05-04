using Dialysis.HIS.Scheduling.Domain;
using Dialysis.HIS.Scheduling.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfAppointmentRepository(HisDbContext db) : IAppointmentRepository
{
    public void Add(Appointment appointment) => db.Appointments.Add(appointment);

    public Task<bool> HasOverlapAsync(Guid resourceId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default) =>
        db.Appointments.AnyAsync(
            a => a.ResourceId == resourceId && a.StartUtc < endUtc && a.EndUtc > startUtc,
            cancellationToken);
}
