using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.IdMapping;

public sealed class CreateIdMappingCommandHandler : ICommandHandler<CreateIdMappingCommand, CreateIdMappingResult>
{
    private readonly IIdMappingRepository _repository;
    private readonly ITenantContext _tenantContext;

    public CreateIdMappingCommandHandler(IIdMappingRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<CreateIdMappingResult> HandleAsync(CreateIdMappingCommand request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var existing = await _repository.GetByLocalAsync(tenantId, request.ResourceType, request.LocalId, request.ExternalSystem, cancellationToken);
        if (existing is not null)
            return new CreateIdMappingResult(null, Conflict: true);

        var mapping = Persistence.Entities.IdMapping.Create(tenantId.Value, request.ResourceType, request.LocalId, request.ExternalSystem, request.ExternalId);
        await _repository.AddAsync(mapping, cancellationToken);

        var dto = new IdMappingDto(mapping.Id.ToString(), mapping.TenantId, mapping.ResourceType, mapping.LocalId, mapping.ExternalSystem, mapping.ExternalId, mapping.CreatedAtUtc);
        return new CreateIdMappingResult(dto, Conflict: false);
    }
}
