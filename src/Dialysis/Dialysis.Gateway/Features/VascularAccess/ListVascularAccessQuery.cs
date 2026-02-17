using Dialysis.Domain.Entities;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.VascularAccess;

public sealed record ListVascularAccessQuery(string PatientId, VascularAccessStatus? Status) : IQuery<ListVascularAccessResult>;

public sealed record ListVascularAccessResult(IReadOnlyList<VascularAccessDto> Items, string? Error);
