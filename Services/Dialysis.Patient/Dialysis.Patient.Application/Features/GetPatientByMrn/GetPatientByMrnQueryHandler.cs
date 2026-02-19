using BuildingBlocks.Tenancy;

using Dialysis.Patient.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Patient.Application.Features.GetPatientByMrn;

internal sealed class GetPatientByMrnQueryHandler : IQueryHandler<GetPatientByMrnQuery, GetPatientByMrnResponse?>
{
    private readonly IPatientReadStore _readStore;
    private readonly ITenantContext _tenant;

    public GetPatientByMrnQueryHandler(IPatientReadStore readStore, ITenantContext tenant)
    {
        _readStore = readStore;
        _tenant = tenant;
    }

    public async Task<GetPatientByMrnResponse?> HandleAsync(GetPatientByMrnQuery request, CancellationToken cancellationToken = default)
    {
        PatientReadDto? dto = await _readStore.GetByMrnAsync(_tenant.TenantId, request.Mrn.Value, cancellationToken);
        return dto is null
            ? null
            : new GetPatientByMrnResponse(
                dto.Id,
                dto.MedicalRecordNumber,
                dto.FirstName,
                dto.LastName,
                dto.DateOfBirth,
                dto.Gender);
    }
}
