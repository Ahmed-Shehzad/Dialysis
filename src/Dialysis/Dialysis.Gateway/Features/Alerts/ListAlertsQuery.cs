using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Alerts;

public sealed record ListAlertsQuery(string? PatientId, bool? ActiveOnly, int? Limit, int Offset) : IQuery<ListAlertsResult>;

public sealed record ListAlertsResult(IReadOnlyList<AlertDto> Items, string? Error);
