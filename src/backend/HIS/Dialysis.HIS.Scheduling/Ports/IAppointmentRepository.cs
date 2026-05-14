using Dialysis.HIS.Scheduling.Domain;

namespace Dialysis.HIS.Scheduling.Ports;

public interface IAppointmentRepository
{
    void Add(Appointment appointment);

    Task<Appointment?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
