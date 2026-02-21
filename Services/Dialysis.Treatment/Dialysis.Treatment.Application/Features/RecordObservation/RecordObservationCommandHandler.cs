using BuildingBlocks.Caching;
using BuildingBlocks.Tenancy;

using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain;
using Dialysis.Treatment.Application.Domain.Services;

using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.RecordObservation;

internal sealed class RecordObservationCommandHandler : ICommandHandler<RecordObservationCommand, RecordObservationResponse>
{
    private const string TreatmentKeyPrefix = "treatment";

    private readonly ITreatmentSessionRepository _repository;
    private readonly VitalSignsMonitoringService _vitalSignsMonitoring;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly ITenantContext _tenant;

    public RecordObservationCommandHandler(ITreatmentSessionRepository repository, VitalSignsMonitoringService vitalSignsMonitoring, ICacheInvalidator cacheInvalidator, ITenantContext tenant)
    {
        _repository = repository;
        _vitalSignsMonitoring = vitalSignsMonitoring;
        _cacheInvalidator = cacheInvalidator;
        _tenant = tenant;
    }

    public async Task<RecordObservationResponse> HandleAsync(RecordObservationCommand request, CancellationToken cancellationToken = default)
    {
        TreatmentSession session = await _repository.GetOrCreateAsync(
            request.SessionId,
            request.PatientMrn,
            request.DeviceId,
            cancellationToken);

        if (request.DeviceEui64 is not null || request.TherapyId is not null)
            session.SetDeviceIdentity(request.DeviceEui64, request.TherapyId);
        if (request.Phase is not null)
            session.SetPhase(request.Phase.Value);

        foreach (ObservationInfo obs in request.Observations)
        {
            var createParams = new ObservationCreateParams(
                obs.Code, obs.Value, obs.Unit, obs.SubId,
                obs.ReferenceRange, obs.ResultStatus, obs.EffectiveTime,
                obs.Provenance, obs.EquipmentInstanceId, obs.Level,
                request.MessageTimeDriftSeconds);
            IReadOnlyList<ThresholdBreach> breaches = _vitalSignsMonitoring.Evaluate(obs.Code, obs.Value);
            _ = session.AddObservation(createParams, breaches);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        await _cacheInvalidator.InvalidateAsync($"{_tenant.TenantId}:{TreatmentKeyPrefix}:{request.SessionId.Value}", cancellationToken);
        return new RecordObservationResponse(request.SessionId, request.Observations.Count);
    }
}
