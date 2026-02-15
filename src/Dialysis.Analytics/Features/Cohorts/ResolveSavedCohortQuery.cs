using Dialysis.Analytics.Services;
using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Cohorts;

public sealed record ResolveSavedCohortQuery(string CohortId) : IQuery<ResolveSavedCohortResult?>;

public sealed record ResolveSavedCohortResult(
    string CohortId,
    string CohortName,
    IReadOnlyList<string> PatientIds,
    IReadOnlyList<string> EncounterIds,
    int TotalPatients,
    int TotalEncounters);
