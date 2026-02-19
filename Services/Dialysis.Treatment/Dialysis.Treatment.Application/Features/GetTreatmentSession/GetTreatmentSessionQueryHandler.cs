using BuildingBlocks.Tenancy;

using Dialysis.Treatment.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.GetTreatmentSession;

internal sealed class GetTreatmentSessionQueryHandler : IQueryHandler<GetTreatmentSessionQuery, GetTreatmentSessionResponse?>
{
    private readonly ITreatmentReadStore _readStore;
    private readonly ITenantContext _tenant;

    public GetTreatmentSessionQueryHandler(ITreatmentReadStore readStore, ITenantContext tenant)
    {
        _readStore = readStore;
        _tenant = tenant;
    }

    public async Task<GetTreatmentSessionResponse?> HandleAsync(GetTreatmentSessionQuery request, CancellationToken cancellationToken = default)
    {
        TreatmentSessionReadDto? dto = await _readStore.GetBySessionIdAsync(_tenant.TenantId, request.SessionId.Value, cancellationToken);
        if (dto is null)
            return null;

        return new GetTreatmentSessionResponse(
            dto.SessionId,
            dto.PatientMrn,
            dto.DeviceId,
            dto.DeviceEui64,
            dto.TherapyId,
            dto.Status,
            dto.StartedAt,
            dto.Observations,
            dto.EndedAt);
    }
}
