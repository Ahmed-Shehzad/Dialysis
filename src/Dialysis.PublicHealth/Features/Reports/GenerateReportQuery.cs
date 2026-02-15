using Dialysis.PublicHealth.Services;
using Intercessor.Abstractions;

namespace Dialysis.PublicHealth.Features.Reports;

public sealed record GenerateReportQuery(
    DateOnly From,
    DateOnly To,
    string Format = "fhir-measure-report",
    string? ConditionCode = null,
    IReadOnlyList<string>? PatientIds = null) : IQuery<ReportResult>;
