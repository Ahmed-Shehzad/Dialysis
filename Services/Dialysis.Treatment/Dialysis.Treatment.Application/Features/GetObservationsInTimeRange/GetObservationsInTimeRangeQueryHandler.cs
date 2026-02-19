using BuildingBlocks.Tenancy;

using Dialysis.Treatment.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.GetObservationsInTimeRange;

internal sealed class GetObservationsInTimeRangeQueryHandler : IQueryHandler<GetObservationsInTimeRangeQuery, GetObservationsInTimeRangeResponse>
{
    private readonly ITreatmentReadStore _readStore;
    private readonly ITenantContext _tenant;

    public GetObservationsInTimeRangeQueryHandler(ITreatmentReadStore readStore, ITenantContext tenant)
    {
        _readStore = readStore;
        _tenant = tenant;
    }

    public async Task<GetObservationsInTimeRangeResponse> HandleAsync(GetObservationsInTimeRangeQuery request, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ObservationReadDto> observations = await _readStore.GetObservationsInTimeRangeAsync(
            _tenant.TenantId,
            request.SessionId,
            request.StartUtc,
            request.EndUtc,
            cancellationToken);

        var dtos = observations.Select(o => new TimeSeriesObservationDto(
            o.Id,
            o.Code,
            o.Value,
            o.Unit,
            o.SubId,
            o.ObservedAtUtc,
            o.EffectiveTime,
            o.ChannelName)).ToList();

        return new GetObservationsInTimeRangeResponse(
            request.SessionId,
            request.StartUtc,
            request.EndUtc,
            dtos);
    }
}
