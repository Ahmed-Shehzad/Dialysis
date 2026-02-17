using Dialysis.Domain.Entities;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.VascularAccess;

public sealed record CreateVascularAccessCommand(
    string PatientId,
    VascularAccessType Type,
    string? Side,
    DateTime? PlacementDate,
    string? Notes) : ICommand<CreateVascularAccessResult>;

public sealed record CreateVascularAccessResult(VascularAccessDto? Dto, string? Error);
