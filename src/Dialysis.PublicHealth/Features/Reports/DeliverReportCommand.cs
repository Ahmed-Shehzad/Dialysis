using Dialysis.PublicHealth.Services;
using Intercessor.Abstractions;

namespace Dialysis.PublicHealth.Features.Reports;

public sealed record DeliverReportCommand(
    DateOnly From,
    DateOnly To,
    string Format = "fhir-measure-report",
    string? ConditionCode = null,
    IReadOnlyList<string>? PatientIds = null) : ICommand<DeliverReportResult>;

public sealed record DeliverReportResult(bool Success, bool Delivered, string? Error);
