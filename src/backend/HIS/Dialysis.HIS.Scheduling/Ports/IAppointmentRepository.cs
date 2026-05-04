using Dialysis.HIS.Scheduling.Domain;

namespace Dialysis.HIS.Scheduling.Ports;

public interface IAppointmentRepository
{
    Task<bool> HasOverlapAsync(Guid resourceId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);

    void Add(Appointment appointment);
}
