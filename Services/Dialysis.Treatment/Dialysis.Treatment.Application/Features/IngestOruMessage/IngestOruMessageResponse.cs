namespace Dialysis.Treatment.Application.Features.IngestOruMessage;

/// <summary>
/// Response after ingesting an ORU message.
/// </summary>
public sealed record IngestOruMessageResponse(string SessionId, int ObservationCount, bool Success);
