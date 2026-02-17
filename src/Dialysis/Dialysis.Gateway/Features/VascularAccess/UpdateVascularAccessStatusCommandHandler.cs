using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.VascularAccess;

public sealed class UpdateVascularAccessStatusCommandHandler : ICommandHandler<UpdateVascularAccessStatusCommand, UpdateVascularAccessStatusResult>
{
    private readonly IVascularAccessRepository _repository;
    private readonly ITenantContext _tenantContext;

    public UpdateVascularAccessStatusCommandHandler(IVascularAccessRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<UpdateVascularAccessStatusResult> HandleAsync(UpdateVascularAccessStatusCommand request, CancellationToken cancellationToken = default)
    {
        if (!Ulid.TryParse(request.Id, out var ulid))
            return new UpdateVascularAccessStatusResult(null, NotFound: true, "Invalid ID format.");

        var tenantId = _tenantContext.TenantId;
        var access = await _repository.GetByIdAsync(tenantId, ulid, cancellationToken);
        if (access is null)
            return new UpdateVascularAccessStatusResult(null, NotFound: true, null);

        access.UpdateStatus(request.Status, request.Notes);
        await _repository.SaveChangesAsync(cancellationToken);

        return new UpdateVascularAccessStatusResult(ToDto(access), NotFound: false, null);
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
