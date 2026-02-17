using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.IdMapping;

public sealed class ListIdMappingsQueryHandler : IQueryHandler<ListIdMappingsQuery, IReadOnlyList<IdMappingDto>>
{
    private readonly IIdMappingRepository _repository;
    private readonly ITenantContext _tenantContext;

    public ListIdMappingsQueryHandler(IIdMappingRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<IdMappingDto>> HandleAsync(ListIdMappingsQuery request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var mappings = await _repository.ListByResourceAsync(tenantId, request.ResourceType, request.Limit, request.Offset, cancellationToken);
        return mappings.Select(m => new IdMappingDto(m.Id.ToString(), m.TenantId, m.ResourceType, m.LocalId, m.ExternalSystem, m.ExternalId, m.CreatedAtUtc)).ToList();
    }
}
