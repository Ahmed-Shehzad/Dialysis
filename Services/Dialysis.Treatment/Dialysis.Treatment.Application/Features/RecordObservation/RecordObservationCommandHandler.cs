using Dialysis.Treatment.Application.Abstractions;

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
        var session = await _repository.GetOrCreateAsync(
            request.SessionId,
            request.PatientMrn,
            request.DeviceId,
            cancellationToken);

        foreach (var obs in request.Observations)
            _ = session.AddObservation(obs.Code, obs.Value, obs.Unit, obs.SubId, obs.Provenance, obs.EffectiveTime);

        await _repository.SaveAsync(session, cancellationToken);
        return new RecordObservationResponse(request.SessionId, request.Observations.Count);
    }
}
