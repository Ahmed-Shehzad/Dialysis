using Intercessor.Abstractions;

namespace Dialysis.Alerting.Features.ProcessAlerts;

public sealed record AcknowledgeAlertCommand : ICommand
{
    public required string AlertId { get; init; }
}
