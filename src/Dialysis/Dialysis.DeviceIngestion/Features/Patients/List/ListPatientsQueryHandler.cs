using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.DeviceIngestion.Features.Patients.List;

public sealed class ListPatientsQueryHandler : IQueryHandler<ListPatientsQuery, IReadOnlyList<Patient>>
{
    private readonly IPatientRepository _patientRepository;

    public ListPatientsQueryHandler(IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;
    }

    public async Task<IReadOnlyList<Patient>> HandleAsync(ListPatientsQuery request, CancellationToken cancellationToken = default)
    {
        return await _patientRepository.ListAsync(
            request.TenantId,
            request.Family,
            request.Given,
            request.Count,
            request.Offset,
            cancellationToken);
    }
}
