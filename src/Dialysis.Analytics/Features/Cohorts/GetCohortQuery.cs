using Dialysis.Analytics.Data;
using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Cohorts;

public sealed record GetCohortQuery(string Id) : IQuery<SavedCohort?>;
