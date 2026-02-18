using Intercessor.Abstractions;

namespace Dialysis.Alarm.Application.Features.IngestOruR40Message;

/// <summary>
/// Ingest a raw HL7 ORU^R40 (PCD-04) alarm message.
/// </summary>
public sealed record IngestOruR40MessageCommand(string RawHl7Message) : ICommand<IngestOruR40MessageResponse>;
