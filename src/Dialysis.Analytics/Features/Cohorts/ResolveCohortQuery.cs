using Dialysis.Analytics.Services;
using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Cohorts;

public sealed record ResolveCohortQuery(CohortCriteria Criteria) : IQuery<CohortResult>;
