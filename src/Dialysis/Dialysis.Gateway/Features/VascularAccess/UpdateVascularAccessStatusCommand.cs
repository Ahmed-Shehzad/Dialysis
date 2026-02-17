using Dialysis.Domain.Entities;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.VascularAccess;

public sealed record UpdateVascularAccessStatusCommand(string Id, VascularAccessStatus Status, string? Notes) : ICommand<UpdateVascularAccessStatusResult>;

public sealed record UpdateVascularAccessStatusResult(VascularAccessDto? Dto, bool NotFound, string? Error);
