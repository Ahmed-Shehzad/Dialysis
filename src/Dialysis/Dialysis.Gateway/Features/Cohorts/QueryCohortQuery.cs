using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Cohorts;

public sealed record CohortResultDto(IReadOnlyList<string> PatientIds, int Total);

public sealed record QueryCohortQuery(
    bool? HasActiveAlert,
    DateTime? SessionFrom,
    DateTime? SessionTo,
    int Limit,
    int Offset) : IQuery<CohortResultDto>;
