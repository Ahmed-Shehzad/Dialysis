namespace Dialysis.Treatment.Application.Features.RecordObservation;

/// <summary>
/// Response after recording observations.
/// </summary>
public sealed record RecordObservationResponse(string SessionId, int ObservationCount);
