using Dialysis.Treatment.Application.Features.GetTreatmentSession;

namespace Dialysis.Treatment.Application.Features.GetTreatmentSessions;

public sealed record GetTreatmentSessionsResponse(IReadOnlyList<TreatmentSessionSummary> Sessions);

public sealed record TreatmentSessionSummary(
    string SessionId,
    string? PatientMrn,
    string? DeviceId,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    IReadOnlyList<ObservationDto> Observations);
