using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Cohorts;

public sealed record ExportCohortQuery(
    bool? HasActiveAlert,
    DateTime? SessionFrom,
    DateTime? SessionTo,
    string Format) : IQuery<ExportCohortResult>;

public sealed record ExportCohortResult(IReadOnlyList<string> PatientIds, string? CsvContent, string ContentType);
