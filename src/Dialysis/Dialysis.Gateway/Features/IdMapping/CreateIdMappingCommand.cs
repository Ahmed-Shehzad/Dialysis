using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.IdMapping;

public sealed record CreateIdMappingCommand(string ResourceType, string LocalId, string ExternalSystem, string ExternalId) : ICommand<CreateIdMappingResult>;

public sealed record CreateIdMappingResult(IdMappingDto? Dto, bool Conflict);
