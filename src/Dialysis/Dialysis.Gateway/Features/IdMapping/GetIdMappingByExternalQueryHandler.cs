using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.IdMapping;

public sealed class GetIdMappingByExternalQueryHandler : IQueryHandler<GetIdMappingByExternalQuery, IdMappingDto?>
{
    private readonly IIdMappingRepository _repository;
    private readonly ITenantContext _tenantContext;

    public GetIdMappingByExternalQueryHandler(IIdMappingRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<IdMappingDto?> HandleAsync(GetIdMappingByExternalQuery request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var mapping = await _repository.GetByExternalAsync(tenantId, request.ResourceType, request.ExternalSystem, request.ExternalId, cancellationToken);
        if (mapping is null)
            return null;

        return new IdMappingDto(mapping.Id.ToString(), mapping.TenantId, mapping.ResourceType, mapping.LocalId, mapping.ExternalSystem, mapping.ExternalId, mapping.CreatedAtUtc);
    }
}
