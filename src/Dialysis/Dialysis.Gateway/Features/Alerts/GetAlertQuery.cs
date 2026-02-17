using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Alerts;

public sealed record GetAlertQuery(string Id) : IQuery<GetAlertResult>;

public sealed record GetAlertResult(AlertDto? Dto, bool InvalidId, bool NotFound);
