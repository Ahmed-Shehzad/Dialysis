namespace Dialysis.SmartConnect.Dicom.Ai;

/// <summary>
/// Pluggable imaging inference model — an edge model or a vendor API behind one port, never a
/// hard-wired vendor. Returns a coded <see cref="ImagingFinding"/> for the (de-identified) request,
/// or <see langword="null"/> when the model declines to read this study (unsupported modality, etc.).
/// </summary>
public interface IImagingInferenceProvider
{
    /// <summary>Stable identifier of the model/version, recorded in the audit trail.</summary>
    string ModelId { get; }

    Task<ImagingFinding?> AnalyzeAsync(ImagingInferenceRequest request, CancellationToken cancellationToken);
}

/// <summary>Audit hook for every AI imaging analysis attempt (governance / bias review / traceability).</summary>
public interface IImagingAiAuditSink
{
    Task RecordAsync(ImagingAiAuditEntry entry, CancellationToken cancellationToken);
}

/// <summary>One audited AI imaging analysis attempt.</summary>
public sealed record ImagingAiAuditEntry(
    string ModelId,
    string StudyInstanceUid,
    string? AccessionNumber,
    bool FindingProduced,
    string? Code,
    double? Confidence,
    DateTimeOffset AtUtc);

/// <summary>No-op audit sink (default). Hosts replace it with a persistent / FHIR AuditEvent sink.</summary>
public sealed class NoopImagingAiAuditSink : IImagingAiAuditSink
{
    public Task RecordAsync(ImagingAiAuditEntry entry, CancellationToken cancellationToken) => Task.CompletedTask;
}
