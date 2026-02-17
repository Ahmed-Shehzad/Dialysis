using Entities = Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.VascularAccess;

public sealed class CreateVascularAccessCommandHandler : ICommandHandler<CreateVascularAccessCommand, CreateVascularAccessResult>
{
    private readonly IVascularAccessRepository _repository;
    private readonly ITenantContext _tenantContext;

    public CreateVascularAccessCommandHandler(IVascularAccessRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<CreateVascularAccessResult> HandleAsync(CreateVascularAccessCommand request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PatientId))
            return new CreateVascularAccessResult(null, "PatientId is required.");

        var tenantId = _tenantContext.TenantId;
        var patientId = new PatientId(request.PatientId);
        var access = Entities.VascularAccess.Create(tenantId, patientId, request.Type, request.Side, request.PlacementDate, request.Notes);
        await _repository.AddAsync(access, cancellationToken);

        return new CreateVascularAccessResult(ToDto(access), null);
    }

    private static VascularAccessDto ToDto(Entities.VascularAccess a) => new(
        a.Id.ToString(),
        a.PatientId.Value,
        a.Type.ToString(),
        a.Side,
        a.PlacementDate,
        a.Status.ToString(),
        a.Notes,
        a.CreatedAtUtc);
}
