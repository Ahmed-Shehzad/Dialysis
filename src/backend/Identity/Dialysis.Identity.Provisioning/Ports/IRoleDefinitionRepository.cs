using Dialysis.Identity.Provisioning.Domain;

namespace Dialysis.Identity.Provisioning.Ports;

public interface IRoleDefinitionRepository
{
    Task<RoleDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RoleDefinition?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleDefinition>> ListAsync(CancellationToken cancellationToken = default);

    void Add(RoleDefinition role);
}
