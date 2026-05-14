using Dialysis.HIS.Security.Domain;

namespace Dialysis.HIS.Security.Ports;

public interface ILocalUserRepository
{
    void Add(LocalUser user);

    Task<LocalUser?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> LoginNameExistsAsync(string loginName, CancellationToken cancellationToken = default);
}
