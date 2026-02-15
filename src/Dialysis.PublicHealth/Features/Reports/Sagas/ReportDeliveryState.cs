using Transponder.Abstractions;
using Transponder.Persistence.Abstractions;

namespace Dialysis.PublicHealth.Features.Reports.Sagas;

/// <summary>
/// Saga state for report generation and delivery orchestration.
/// </summary>
public sealed class ReportDeliveryState : ISagaStatusState
{
    public Ulid CorrelationId { get; set; }
    public Ulid? ConversationId { get; set; }
    public int Version { get; set; }
    public SagaStatus Status { get; set; }

    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public string Format { get; set; } = "fhir-measure-report";
    public string? ConditionCode { get; set; }
    public IReadOnlyList<string>? PatientIds { get; set; }

    public byte[]? ReportContent { get; set; }
    public string? ContentType { get; set; }
    public string? Filename { get; set; }
}
