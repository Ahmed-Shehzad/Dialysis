using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.IngestOruMessage;

/// <summary>
/// Ingest a raw HL7 ORU^R01 (PCD-01) message.
/// </summary>
public sealed record IngestOruMessageCommand(string RawHl7Message) : ICommand<IngestOruMessageResponse>;
