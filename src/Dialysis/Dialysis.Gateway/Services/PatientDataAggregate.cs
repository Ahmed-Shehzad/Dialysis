using Dialysis.Domain.Aggregates;
using Dialysis.Domain.Entities;

namespace Dialysis.Gateway.Services;

/// <summary>
/// Aggregated patient data for export/EHR push. Single purpose: hold all patient-related data in one structure.
/// </summary>
public sealed record PatientDataAggregate(
    Patient Patient,
    IReadOnlyList<Observation> Observations,
    IReadOnlyList<Session> Sessions,
    IReadOnlyList<Condition> Conditions,
    IReadOnlyList<EpisodeOfCare> Episodes);
