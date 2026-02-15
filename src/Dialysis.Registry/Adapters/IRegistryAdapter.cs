namespace Dialysis.Registry.Adapters;

/// <summary>Adapter for registry-specific export formats (ESRD, QIP, CROWNWeb, etc.).</summary>
public interface IRegistryAdapter
{
    string Name { get; }
    Task<RegistryExportResult> ExportAsync(RegistryExportRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Output format for registry export.</summary>
public static class RegistryOutputFormat
{
    public const string NdJson = "ndjson";
    public const string Csv = "csv";
    public const string Hl7V2 = "hl7v2";
}

public sealed record RegistryExportRequest(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<string>? PatientIds = null,
    string? TenantId = null,
    string? OutputFormat = null);

public sealed record RegistryExportResult(
    bool Success,
    string Format,
    Stream? Content,
    string? Filename,
    string? Error);
