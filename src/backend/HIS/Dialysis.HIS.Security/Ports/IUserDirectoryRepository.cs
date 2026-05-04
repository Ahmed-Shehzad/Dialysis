using Dialysis.HIS.Security.Domain;

namespace Dialysis.HIS.Security.Ports;

public interface IUserDirectoryRepository
{
    Task<HisUserAccount?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    Task<HisRole?> FindRoleByCodeAsync(string code, CancellationToken cancellationToken = default);

    void AddUser(HisUserAccount user);

    void AttachRole(HisUserAccount user, HisRole role);
}
