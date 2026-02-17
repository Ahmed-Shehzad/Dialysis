using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Alerts;

public sealed record AcknowledgeAlertCommand(string Id, string? AcknowledgedBy) : ICommand<AcknowledgeAlertResult>;

public sealed record AcknowledgeAlertResult(AlertDto? Dto, bool NotFound, bool InvalidId);
