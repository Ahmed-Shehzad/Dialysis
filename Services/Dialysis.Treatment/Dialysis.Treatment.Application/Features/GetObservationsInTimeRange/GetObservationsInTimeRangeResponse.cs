namespace Dialysis.Treatment.Application.Features.GetObservationsInTimeRange;

public sealed record GetObservationsInTimeRangeResponse(
    string SessionId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    IReadOnlyList<TimeSeriesObservationDto> Observations);

public sealed record TimeSeriesObservationDto(
    string Id,
    string Code,
    string? Value,
    string? Unit,
    string? SubId,
    DateTimeOffset ObservedAtUtc,
    DateTimeOffset? EffectiveTime,
    string? ChannelName);
