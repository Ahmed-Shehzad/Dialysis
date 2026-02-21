namespace Dialysis.Treatment.Infrastructure.ReadModels;

public sealed class PreAssessmentReadModel
{
    public string Id { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public decimal? PreWeightKg { get; init; }
    public int? BpSystolic { get; init; }
    public int? BpDiastolic { get; init; }
    public string? AccessTypeValue { get; init; }
    public bool PrescriptionConfirmed { get; init; }
    public string? PainSymptomNotes { get; init; }
    public DateTimeOffset RecordedAt { get; init; }
    public string? RecordedBy { get; init; }
}
