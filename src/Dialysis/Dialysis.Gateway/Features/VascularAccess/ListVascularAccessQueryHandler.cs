using Entities = Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.VascularAccess;

public sealed class ListVascularAccessQueryHandler : IQueryHandler<ListVascularAccessQuery, ListVascularAccessResult>
{
    private readonly IVascularAccessRepository _repository;
    private readonly ITenantContext _tenantContext;

    public ListVascularAccessQueryHandler(IVascularAccessRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<ListVascularAccessResult> HandleAsync(ListVascularAccessQuery request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PatientId))
            return new ListVascularAccessResult([], "patientId is required.");

        var tenantId = _tenantContext.TenantId;
        var pid = new PatientId(request.PatientId);
        var list = await _repository.GetByPatientAsync(tenantId, pid, request.Status, cancellationToken);
        return new ListVascularAccessResult(list.Select(ToDto).ToList(), null);
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
