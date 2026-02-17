using EpisodeOfCareEntity = Dialysis.Domain.Entities.EpisodeOfCare;
using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Fhir.EpisodeOfCare;

public sealed record SearchEpisodesOfCareQuery(string PatientId) : IQuery<IReadOnlyList<EpisodeOfCareEntity>>;
