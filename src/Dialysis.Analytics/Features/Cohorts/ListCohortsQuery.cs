using Dialysis.Analytics.Data;
using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Cohorts;

public sealed record ListCohortsQuery : IQuery<IReadOnlyList<CohortSummaryDto>>;

public sealed record CohortSummaryDto(string Id, string Name, CohortCriteria Criteria, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
