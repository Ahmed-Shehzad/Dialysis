using Dialysis.EHR.Scheduling.Domain;

namespace Dialysis.EHR.Scheduling.Ports;

public interface IAppointmentRepository
{
    Task<Appointment?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> HasOverlapAsync(Guid providerId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);

    void Add(Appointment appointment);
}
