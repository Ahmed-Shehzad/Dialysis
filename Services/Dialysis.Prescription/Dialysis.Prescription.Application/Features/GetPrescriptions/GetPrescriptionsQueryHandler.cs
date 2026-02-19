using Dialysis.Prescription.Application.Abstractions;
using Dialysis.Prescription.Application.Services;

using Intercessor.Abstractions;

namespace Dialysis.Prescription.Application.Features.GetPrescriptions;

internal sealed class GetPrescriptionsQueryHandler : IQueryHandler<GetPrescriptionsQuery, GetPrescriptionsResponse>
{
    private readonly IPrescriptionRepository _repository;

    public GetPrescriptionsQueryHandler(IPrescriptionRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetPrescriptionsResponse> HandleAsync(GetPrescriptionsQuery request, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Domain.Prescription> prescriptions = request.Subject is { } mrn
            ? await _repository.GetByPatientMrnAsync(mrn, request.Limit, cancellationToken)
            : await _repository.GetAllForTenantAsync(request.Limit, cancellationToken);
        var summaries = prescriptions.Select(p => new PrescriptionSummary(
            p.OrderId,
            p.PatientMrn.Value,
            p.Modality,
            p.OrderingProvider,
            PrescriptionSettingResolver.GetBloodFlowRateMlMin(p.Settings),
            PrescriptionSettingResolver.GetUfRateMlH(p.Settings),
            PrescriptionSettingResolver.GetUfTargetVolumeMl(p.Settings),
            p.ReceivedAt)).ToList();
        return new GetPrescriptionsResponse(summaries);
    }
}
