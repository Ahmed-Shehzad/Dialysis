namespace Dialysis.Treatment.Application.Features.IngestOruBatch;

/// <summary>
/// Result of batch ORU ingestion (run sheet capture).
/// </summary>
public sealed record IngestOruBatchResponse(int ProcessedCount, IReadOnlyList<string> SessionIds);
