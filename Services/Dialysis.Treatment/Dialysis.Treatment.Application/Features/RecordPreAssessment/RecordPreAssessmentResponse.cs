namespace Dialysis.Treatment.Application.Features.RecordPreAssessment;

public sealed record RecordPreAssessmentResponse(string SessionId, DateTimeOffset RecordedAt);
