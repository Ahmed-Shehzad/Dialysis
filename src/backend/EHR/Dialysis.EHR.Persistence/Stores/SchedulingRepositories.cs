using Dialysis.EHR.Scheduling.Domain;
using Dialysis.EHR.Scheduling.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Stores;

public sealed class AppointmentRepository(EhrDbContext db) : IAppointmentRepository
{
    public Task<Appointment?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Appointments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> HasOverlapAsync(Guid providerId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default) =>
        db.Appointments
            .Where(a => a.ProviderId == providerId
                && a.Status != AppointmentStatus.Cancelled
                && a.Status != AppointmentStatus.NoShow
                && a.StartUtc < endUtc
                && a.EndUtc > startUtc)
            .AnyAsync(cancellationToken);

    public void Add(Appointment appointment) => db.Appointments.Add(appointment);
}

public sealed class ProviderAvailabilityRepository(EhrDbContext db) : IProviderAvailabilityRepository
{
    public async Task<IReadOnlyList<ProviderAvailabilityWindow>> ListByProviderAsync(Guid providerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
        await db.ProviderAvailabilityWindows
            .Where(w => w.ProviderId == providerId && w.IsActive && w.StartUtc < toUtc && w.EndUtc > fromUtc)
            .OrderBy(w => w.StartUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(ProviderAvailabilityWindow window) => db.ProviderAvailabilityWindows.Add(window);
}
