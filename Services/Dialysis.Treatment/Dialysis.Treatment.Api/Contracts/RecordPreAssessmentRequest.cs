namespace Dialysis.Treatment.Api.Contracts;

public sealed record RecordPreAssessmentRequest(
    decimal? PreWeightKg,
    int? BpSystolic,
    int? BpDiastolic,
    string? AccessTypeValue,
    bool PrescriptionConfirmed,
    string? PainSymptomNotes,
    string? RecordedBy);
