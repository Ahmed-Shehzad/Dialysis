using EpisodeOfCareEntity = Dialysis.Domain.Entities.EpisodeOfCare;
using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Fhir.EpisodeOfCare;

public sealed record CreateEpisodeOfCareCommand(string FhirJson) : ICommand<CreateEpisodeOfCareResult>;

public sealed record CreateEpisodeOfCareResult(EpisodeOfCareEntity? Episode, string? Error);
