using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Orders;

public sealed class ListOrdersQueryHandler : IQueryHandler<ListOrdersQuery, IReadOnlyList<ServiceRequest>>
{
    private readonly IServiceRequestRepository _repository;
    private readonly ITenantContext _tenantContext;

    public ListOrdersQueryHandler(IServiceRequestRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<ServiceRequest>> HandleAsync(ListOrdersQuery request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var patientId = new PatientId(request.PatientId);
        return await _repository.ListByPatientAsync(
            tenantId,
            patientId,
            request.Status,
            request.Limit,
            request.Offset,
            cancellationToken);
    }
}
