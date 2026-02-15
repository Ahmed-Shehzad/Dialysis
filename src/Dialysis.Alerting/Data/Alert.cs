namespace Dialysis.Alerting.Data;

public sealed class Alert
{
    public required string Id { get; init; }
    public required string PatientId { get; init; }
    public required string EncounterId { get; init; }
    public required string Code { get; init; }
    public required string Severity { get; init; }
    public string? Message { get; init; }
    public AlertStatus Status { get; set; }
    public DateTimeOffset RaisedAt { get; init; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
}

public enum AlertStatus
{
    Active = 0,
    Acknowledged = 1
}
