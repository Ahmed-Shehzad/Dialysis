using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.GetObservationsInTimeRange;

public sealed record GetObservationsInTimeRangeQuery(
    string SessionId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc) : IQuery<GetObservationsInTimeRangeResponse>;
