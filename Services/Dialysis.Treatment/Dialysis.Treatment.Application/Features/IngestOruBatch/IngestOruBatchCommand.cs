using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.IngestOruBatch;

public sealed record IngestOruBatchCommand(string RawHl7Batch) : ICommand<IngestOruBatchResponse>;
