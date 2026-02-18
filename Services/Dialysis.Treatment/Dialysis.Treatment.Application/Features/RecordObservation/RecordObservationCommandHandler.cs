using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain;

using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.RecordObservation;

internal sealed class RecordObservationCommandHandler : ICommandHandler<RecordObservationCommand, RecordObservationResponse>
{
    private readonly ITreatmentSessionRepository _repository;

    public RecordObservationCommandHandler(ITreatmentSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<RecordObservationResponse> HandleAsync(RecordObservationCommand request, CancellationToken cancellationToken = default)
    {
        TreatmentSession session = await _repository.GetOrCreateAsync(
            request.SessionId,
            request.PatientMrn,
            request.DeviceId,
            cancellationToken);

        if (request.Phase is not null)
            session.SetPhase(request.Phase.Value);

        foreach (ObservationInfo obs in request.Observations)
        {
            var createParams = new ObservationCreateParams(
                obs.Code, obs.Value, obs.Unit, obs.SubId,
                obs.ReferenceRange, obs.ResultStatus, obs.EffectiveTime,
                obs.Provenance, obs.EquipmentInstanceId, obs.Level);
            _ = session.AddObservation(createParams);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return new RecordObservationResponse(request.SessionId, request.Observations.Count);
    }
}
