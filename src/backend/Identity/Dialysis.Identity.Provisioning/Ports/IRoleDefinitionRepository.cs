using Dialysis.Identity.Provisioning.Domain;

namespace Dialysis.Identity.Provisioning.Ports;

public interface IRoleDefinitionRepository
{
    Task<RoleDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RoleDefinition?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleDefinition>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns only the roles whose ids are in <paramref name="ids"/>. Lets callers that already
    /// know the assigned role ids (e.g. permission resolution) push the filter into the database
    /// instead of loading every role and filtering in memory.
    /// </summary>
    Task<IReadOnlyList<RoleDefinition>> ListByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    void Add(RoleDefinition role);
}
