using Intercessor.Abstractions;

namespace Dialysis.DeviceIngestion.Features.Vitals.Ingest;

/// <summary>
/// Ingest vitals from raw device output via adapter. Phase 1.1.4.
/// </summary>
public sealed record RawIngestVitalsCommand(string RawPayload, string AdapterId) : ICommand<RawIngestVitalsResult>;

public sealed record RawIngestVitalsResult(bool Success, string? ObservationId, string? Error);
