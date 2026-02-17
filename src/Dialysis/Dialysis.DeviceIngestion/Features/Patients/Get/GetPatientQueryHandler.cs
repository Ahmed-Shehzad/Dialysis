using Dialysis.Persistence.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.DeviceIngestion.Features.Patients.Get;

public sealed class GetPatientQueryHandler : IQueryHandler<GetPatientQuery, Domain.Entities.Patient?>
{
    private readonly IPatientRepository _patientRepository;

    public GetPatientQueryHandler(IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;
    }

    public async Task<Domain.Entities.Patient?> HandleAsync(GetPatientQuery request, CancellationToken cancellationToken = default)
    {
        return await _patientRepository.GetByIdAsync(request.TenantId, request.LogicalId, cancellationToken);
    }
}
