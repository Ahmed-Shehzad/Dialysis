using Dialysis.PublicHealth.Models;
using Intercessor.Abstractions;

namespace Dialysis.PublicHealth.Features.ReportableConditions;

/// <param name="Jurisdiction">Optional filter: US, DE, UK, etc.</param>
public sealed record ListReportableConditionsQuery(string? Jurisdiction = null) : IQuery<IReadOnlyList<ReportableCondition>>;
