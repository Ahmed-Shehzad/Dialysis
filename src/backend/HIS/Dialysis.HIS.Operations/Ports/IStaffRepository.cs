using Dialysis.HIS.Operations.Domain;

namespace Dialysis.HIS.Operations.Ports;

public interface IStaffRepository
{
    Task<StaffMember?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Update(StaffMember member);
}
