using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Health;

public sealed record GetHealthQuery : IQuery<HealthResult>;

public sealed record HealthResult(string Status, DateTimeOffset Timestamp);
