using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain.ValueObjects;
using Dialysis.Treatment.Application.Features.GetTreatmentSession;

using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.GetTreatmentSessions;

internal sealed class GetTreatmentSessionsQueryHandler : IQueryHandler<GetTreatmentSessionsQuery, GetTreatmentSessionsResponse>
{
    private readonly ITreatmentSessionRepository _repository;

    public GetTreatmentSessionsQueryHandler(ITreatmentSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetTreatmentSessionsResponse> HandleAsync(GetTreatmentSessionsQuery request, CancellationToken cancellationToken = default)
    {
        var sessions = request.Subject is not null || request.DateFrom.HasValue || request.DateTo.HasValue
            ? await _repository.SearchForFhirAsync(request.Subject, request.DateFrom, request.DateTo, request.Limit, cancellationToken)
            : await _repository.GetAllForTenantAsync(request.Limit, cancellationToken);
        var summaries = sessions.Select(s => new TreatmentSessionSummary(
            s.SessionId.Value,
            s.PatientMrn?.Value,
            s.DeviceId?.Value,
            s.Status.Value,
            s.StartedAt,
            s.EndedAt,
            s.Observations.Select(o =>
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
            }).ToList())).ToList();
        return new GetTreatmentSessionsResponse(summaries);
    }
}
