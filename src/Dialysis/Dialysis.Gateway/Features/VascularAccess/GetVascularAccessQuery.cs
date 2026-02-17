using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.VascularAccess;

public sealed record GetVascularAccessQuery(string Id) : IQuery<GetVascularAccessResult>;

public sealed record GetVascularAccessResult(VascularAccessDto? Dto, bool InvalidId, bool NotFound);
