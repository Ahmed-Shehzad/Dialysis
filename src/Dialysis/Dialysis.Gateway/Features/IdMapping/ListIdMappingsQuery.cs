using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.IdMapping;

public sealed record ListIdMappingsQuery(string ResourceType, int Limit, int Offset) : IQuery<IReadOnlyList<IdMappingDto>>;
