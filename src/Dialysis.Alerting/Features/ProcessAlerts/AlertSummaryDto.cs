namespace Dialysis.Alerting.Features.ProcessAlerts;

public sealed record AlertSummaryDto
{
    public required string Id { get; init; }
    public required string PatientId { get; init; }
    public required string EncounterId { get; init; }
    public required string Code { get; init; }
    public required string Severity { get; init; }
    public string? Message { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset RaisedAt { get; init; }
    public DateTimeOffset? AcknowledgedAt { get; init; }
}
