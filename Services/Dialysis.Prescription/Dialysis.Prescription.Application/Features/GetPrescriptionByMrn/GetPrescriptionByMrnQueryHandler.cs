using BuildingBlocks.Tenancy;

using Dialysis.Prescription.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Prescription.Application.Features.GetPrescriptionByMrn;

internal sealed class GetPrescriptionByMrnQueryHandler : IQueryHandler<GetPrescriptionByMrnQuery, GetPrescriptionByMrnResponse?>
{
    private readonly IPrescriptionReadStore _readStore;
    private readonly ITenantContext _tenant;

    public GetPrescriptionByMrnQueryHandler(IPrescriptionReadStore readStore, ITenantContext tenant)
    {
        _readStore = readStore;
        _tenant = tenant;
    }

    public async Task<GetPrescriptionByMrnResponse?> HandleAsync(GetPrescriptionByMrnQuery request, CancellationToken cancellationToken = default)
    {
        PrescriptionReadDto? dto = await _readStore.GetLatestByMrnAsync(_tenant.TenantId, request.Mrn, cancellationToken);
        if (dto is null) return null;

        return new GetPrescriptionByMrnResponse(
            dto.OrderId,
            dto.Modality ?? string.Empty,
            dto.BloodFlowRateMlMin,
            dto.UfTargetVolumeMl,
            dto.UfRateMlH);
    }
}
