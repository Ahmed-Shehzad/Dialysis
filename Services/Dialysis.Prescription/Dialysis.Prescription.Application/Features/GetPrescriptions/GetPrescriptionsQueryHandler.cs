using BuildingBlocks.Tenancy;

using Dialysis.Prescription.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Prescription.Application.Features.GetPrescriptions;

internal sealed class GetPrescriptionsQueryHandler : IQueryHandler<GetPrescriptionsQuery, GetPrescriptionsResponse>
{
    private readonly IPrescriptionReadStore _readStore;
    private readonly ITenantContext _tenant;

    public GetPrescriptionsQueryHandler(IPrescriptionReadStore readStore, ITenantContext tenant)
    {
        _readStore = readStore;
        _tenant = tenant;
    }

    public async Task<GetPrescriptionsResponse> HandleAsync(GetPrescriptionsQuery request, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PrescriptionReadDto> dtos = request.Subject is { } mrn
            ? await _readStore.GetByPatientMrnAsync(_tenant.TenantId, mrn.Value, request.Limit, cancellationToken)
            : await _readStore.GetAllForTenantAsync(_tenant.TenantId, request.Limit, cancellationToken);
        var summaries = dtos.Select(d => new PrescriptionSummary(
            d.OrderId,
            d.PatientMrn,
            d.Modality,
            d.OrderingProvider,
            d.BloodFlowRateMlMin,
            d.UfRateMlH,
            d.UfTargetVolumeMl,
            d.ReceivedAt)).ToList();
        return new GetPrescriptionsResponse(summaries);
    }
}
