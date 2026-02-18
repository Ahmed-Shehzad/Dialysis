using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain;
using Dialysis.Treatment.Application.Domain.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.GetTreatmentSession;

internal sealed class GetTreatmentSessionQueryHandler : IQueryHandler<GetTreatmentSessionQuery, GetTreatmentSessionResponse?>
{
    private readonly ITreatmentSessionRepository _repository;

    public GetTreatmentSessionQueryHandler(ITreatmentSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetTreatmentSessionResponse?> HandleAsync(GetTreatmentSessionQuery request, CancellationToken cancellationToken = default)
    {
        TreatmentSession? session = await _repository.GetBySessionIdAsync(request.SessionId, cancellationToken);
        if (session is null)
            return null;

        var observations = session.Observations
            .Select(o =>
            {
                string? channelName = null;
                if (ContainmentPath.TryParse(o.SubId) is { } path && path.ChannelId is { } cid)
                    channelName = ContainmentPath.GetChannelName(cid);
                return new ObservationDto(
                    o.Code.Value,
                    o.Value,
                    o.Unit,
                    o.SubId,
                    o.ReferenceRange,
                    o.Provenance,
                    o.EffectiveTime,
                    channelName);
            })
            .ToList();

        return new GetTreatmentSessionResponse(
            session.SessionId.Value,
            session.PatientMrn?.Value,
            session.DeviceId?.Value,
            session.DeviceEui64,
            session.TherapyId,
            session.Status.Value,
            session.StartedAt,
            observations,
            session.EndedAt);
    }
}
