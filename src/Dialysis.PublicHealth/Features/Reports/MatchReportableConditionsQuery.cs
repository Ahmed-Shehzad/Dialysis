using Dialysis.PublicHealth.Services;
using Hl7.Fhir.Model;
using Intercessor.Abstractions;

namespace Dialysis.PublicHealth.Features.Reports;

public sealed record MatchReportableConditionsQuery(
    Resource Resource,
    string? Jurisdiction = null) : IQuery<IReadOnlyList<ReportableConditionMatch>>;
