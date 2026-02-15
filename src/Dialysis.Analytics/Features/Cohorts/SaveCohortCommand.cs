using Dialysis.Analytics.Data;
using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Cohorts;

public sealed record SaveCohortCommand(string? Id, string? Name, CohortCriteria? Criteria) : ICommand<SavedCohort>;
