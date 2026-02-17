using EpisodeOfCareEntity = Dialysis.Domain.Entities.EpisodeOfCare;
using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Fhir.EpisodeOfCare;

public sealed record GetEpisodeOfCareQuery(string Id) : IQuery<EpisodeOfCareEntity?>;
