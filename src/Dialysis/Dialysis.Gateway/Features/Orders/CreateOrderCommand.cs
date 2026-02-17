using Dialysis.Domain.Entities;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Orders;

public sealed record CreateOrderCommand(
    string PatientId,
    string Code,
    string? Display,
    string? Intent = "order",
    string? EncounterId = null,
    string? SessionId = null,
    string? ReasonText = null,
    string? RequesterId = null,
    string? Frequency = null,
    string? Category = null
) : ICommand<CreateOrderResult>;

public sealed record CreateOrderResult(ServiceRequest? Order, string? Error);
