using Dialysis.Analytics.Services;
using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Export;

public sealed record ExportCohortCommand(
    CohortResult Cohort,
    string ResourceType,
    ExportFormat Format,
    Stream Output) : ICommand;
