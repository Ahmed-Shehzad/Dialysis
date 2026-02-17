using Dialysis.Domain.Aggregates;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Meds;

public sealed class ListMedicationsQueryHandler : IQueryHandler<ListMedicationsQuery, IReadOnlyList<MedicationAdministration>>
{
    private readonly IMedicationAdministrationRepository _repository;
    private readonly ITenantContext _tenantContext;

    public ListMedicationsQueryHandler(IMedicationAdministrationRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<MedicationAdministration>> HandleAsync(ListMedicationsQuery request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var patientId = new PatientId(request.PatientId);

        if (!string.IsNullOrWhiteSpace(request.SessionId))
            return await _repository.ListBySessionAsync(tenantId, request.SessionId, cancellationToken);

        return await _repository.ListByPatientAsync(tenantId, patientId, request.Limit, request.Offset, cancellationToken);
    }
}
