using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

/// <summary>CRUD over named outbound endpoints.</summary>
public interface IEndpointRepository
{
    Task<EndpointEntity?> GetByNameAsync(string name, CancellationToken cancellationToken);

    Task<EndpointEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<EndpointEntity>> ListAsync(CancellationToken cancellationToken);

    Task AddAsync(EndpointEntity entity, CancellationToken cancellationToken);

    Task UpdateAsync(EndpointEntity entity, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
