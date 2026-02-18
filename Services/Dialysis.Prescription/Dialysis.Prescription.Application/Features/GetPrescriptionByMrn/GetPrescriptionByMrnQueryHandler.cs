using BuildingBlocks.ValueObjects;

using Dialysis.Prescription.Application.Abstractions;
using Dialysis.Prescription.Application.Services;

using Intercessor.Abstractions;

using PrescriptionEntity = Dialysis.Prescription.Application.Domain.Prescription;

namespace Dialysis.Prescription.Application.Features.GetPrescriptionByMrn;

internal sealed class GetPrescriptionByMrnQueryHandler : IQueryHandler<GetPrescriptionByMrnQuery, GetPrescriptionByMrnResponse?>
{
    private readonly IPrescriptionRepository _repository;

    public GetPrescriptionByMrnQueryHandler(IPrescriptionRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetPrescriptionByMrnResponse?> HandleAsync(GetPrescriptionByMrnQuery request, CancellationToken cancellationToken = default)
    {
        var mrn = new MedicalRecordNumber(request.Mrn);
        PrescriptionEntity? prescription = await _repository.GetLatestByMrnAsync(mrn, cancellationToken);
        if (prescription is null) return null;

        decimal? bloodFlow = PrescriptionSettingResolver.GetBloodFlowRateMlMin(prescription.Settings);
        decimal? ufTarget = PrescriptionSettingResolver.GetUfTargetVolumeMl(prescription.Settings);
        decimal? ufRate = PrescriptionSettingResolver.GetUfRateMlH(prescription.Settings);

        return new GetPrescriptionByMrnResponse(
            prescription.OrderId,
            prescription.Modality ?? string.Empty,
            bloodFlow,
            ufTarget,
            ufRate);
    }
}
