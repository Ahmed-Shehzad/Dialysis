using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;
using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.VascularAccess;

public sealed class GetVascularAccessQueryHandler : IQueryHandler<GetVascularAccessQuery, GetVascularAccessResult>
{
    private readonly IVascularAccessRepository _repository;
    private readonly ITenantContext _tenantContext;

    public GetVascularAccessQueryHandler(IVascularAccessRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<GetVascularAccessResult> HandleAsync(GetVascularAccessQuery request, CancellationToken cancellationToken = default)
    {
        if (!Ulid.TryParse(request.Id, out var ulid))
            return new GetVascularAccessResult(null, InvalidId: true, NotFound: false);

        var tenantId = _tenantContext.TenantId;
        var access = await _repository.GetByIdAsync(tenantId, ulid, cancellationToken);
        if (access is null)
            return new GetVascularAccessResult(null, InvalidId: false, NotFound: true);

        return new GetVascularAccessResult(ToDto(access), InvalidId: false, NotFound: false);
    }

    private static VascularAccessDto ToDto(Domain.Entities.VascularAccess a) => new(
        a.Id.ToString(),
        a.PatientId.Value,
        a.Type.ToString(),
        a.Side,
        a.PlacementDate,
        a.Status.ToString(),
        a.Notes,
        a.CreatedAtUtc);
}
