using Dialysis.Identity.Provisioning.Domain;

namespace Dialysis.Identity.Provisioning.Ports;

public interface IUserAccountRepository
{
    Task<UserAccount?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserAccount?> FindBySubjectAsync(string subject, CancellationToken cancellationToken = default);

    void Add(UserAccount user);

    void Update(UserAccount user);
}
