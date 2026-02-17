using Dialysis.Domain.Entities;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Orders;

public sealed record ListOrdersQuery(
    string PatientId,
    string? Status = null,
    int Limit = 50,
    int Offset = 0
) : IQuery<IReadOnlyList<ServiceRequest>>;
