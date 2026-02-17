using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Hl7;

public sealed record ListFailedHl7Query(int Limit, int Offset) : IQuery<IReadOnlyList<FailedHl7MessageDto>>;
