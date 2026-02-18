using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain;
using Dialysis.Treatment.Application.Domain.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.GetObservationsInTimeRange;

internal sealed class GetObservationsInTimeRangeQueryHandler : IQueryHandler<GetObservationsInTimeRangeQuery, GetObservationsInTimeRangeResponse>
{
    private readonly ITreatmentSessionRepository _repository;

    public GetObservationsInTimeRangeQueryHandler(ITreatmentSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetObservationsInTimeRangeResponse> HandleAsync(GetObservationsInTimeRangeQuery request, CancellationToken cancellationToken = default)
    {
        var sessionId = new SessionId(request.SessionId);
        IReadOnlyList<Observation> observations = await _repository.GetObservationsInTimeRangeAsync(
            sessionId,
            request.StartUtc,
            request.EndUtc,
            cancellationToken);

        var dtos = observations.Select(o =>
        {
            string? channelName = null;
            if (ContainmentPath.TryParse(o.SubId) is { } path && path.ChannelId is { } cid)
                channelName = ContainmentPath.GetChannelName(cid);
            return new TimeSeriesObservationDto(
                o.Id.ToString(),
                o.Code.Value,
                o.Value,
                o.Unit,
                o.SubId,
                o.ObservedAtUtc,
                o.EffectiveTime,
                channelName);
        }).ToList();

        return new GetObservationsInTimeRangeResponse(
            request.SessionId,
            request.StartUtc,
            request.EndUtc,
            dtos);
    }
}
