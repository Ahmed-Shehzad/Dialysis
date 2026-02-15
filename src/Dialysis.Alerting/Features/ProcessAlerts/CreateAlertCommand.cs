using Intercessor.Abstractions;

namespace Dialysis.Alerting.Features.ProcessAlerts;

public sealed record CreateAlertCommand : ICommand<CreateAlertResult>
{
    public required string PatientId { get; init; }
    public required string EncounterId { get; init; }
    public required string Code { get; init; }
    public required string Severity { get; init; }
    public string? Message { get; init; }
}

public sealed record CreateAlertResult
{
    public required string AlertId { get; init; }
}
