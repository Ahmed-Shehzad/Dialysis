using Dialysis.HIS.Scheduling.Domain;

namespace Dialysis.HIS.Scheduling.Ports;

public interface ISchedulingResourceDirectory
{
    Task<SchedulingResource?> GetAsync(Guid resourceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchedulingResource>> ListAsync(string? kindCode, CancellationToken cancellationToken = default);
}
