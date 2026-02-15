namespace Dialysis.PublicHealth.Services;

/// <summary>Design: generates public health / registry reports from FHIR data.</summary>
public interface IReportGenerator
{
    /// <summary>Report format (e.g. CMS ESRD, HL7 v2, FHIR MeasureReport).</summary>
    string Format { get; }

    /// <summary>Generate a report for the given period and optional cohort.</summary>
    Task<ReportResult> GenerateAsync(ReportRequest request, CancellationToken cancellationToken = default);
}

public sealed record ReportRequest(
    DateOnly From,
    DateOnly To,
    string? ConditionCode = null,
    IReadOnlyList<string>? PatientIds = null,
    string? TenantId = null);

public sealed record ReportResult(
    bool Success,
    string Format,
    Stream? Content,
    string? Filename,
    string? Error);