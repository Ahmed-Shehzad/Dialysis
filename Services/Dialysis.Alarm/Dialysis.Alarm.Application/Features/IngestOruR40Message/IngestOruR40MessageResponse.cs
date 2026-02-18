namespace Dialysis.Alarm.Application.Features.IngestOruR40Message;

/// <summary>
/// Response after ingesting an ORU^R40 message.
/// </summary>
public sealed record IngestOruR40MessageResponse(int AlarmCount, IReadOnlyList<string> AlarmIds);
