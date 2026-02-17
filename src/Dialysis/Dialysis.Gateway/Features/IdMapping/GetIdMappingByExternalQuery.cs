using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.IdMapping;

public sealed record GetIdMappingByExternalQuery(string ResourceType, string ExternalSystem, string ExternalId) : IQuery<IdMappingDto?>;
