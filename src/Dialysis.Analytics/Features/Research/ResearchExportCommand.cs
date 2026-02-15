using Dialysis.Analytics.Features.Cohorts;
using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Research;

public sealed record ResearchExportCommand(
    string? CohortId,
    CohortCriteria? Criteria,
    string ResourceType = "Patient",
    string Level = "Basic",
    Stream? Output = null) : ICommand<ResearchExportResult>;

public sealed record ResearchExportResult(bool Success, long Count, string? Error);
