using Dialysis.Treatment.Application.Domain.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.RecordPreAssessment;

public sealed record RecordPreAssessmentCommand(
    SessionId SessionId,
    decimal? PreWeightKg,
    int? BpSystolic,
    int? BpDiastolic,
    AccessType? AccessTypeValue,
    bool PrescriptionConfirmed,
    string? PainSymptomNotes,
    string? RecordedBy = null) : ICommand<RecordPreAssessmentResponse>;
