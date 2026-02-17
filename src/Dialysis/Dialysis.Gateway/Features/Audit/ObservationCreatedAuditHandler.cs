using Dialysis.Contracts.Events;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

namespace Dialysis.Gateway.Features.Audit;

/// <summary>
/// C5: Records audit when an observation (vitals, labs) is created.
/// </summary>
public sealed class ObservationCreatedAuditHandler : INotificationHandler<ObservationCreated>
{
    private readonly ISender _sender;
    private readonly ILogger<ObservationCreatedAuditHandler> _logger;

    public ObservationCreatedAuditHandler(ISender sender, ILogger<ObservationCreatedAuditHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task HandleAsync(ObservationCreated notification, CancellationToken cancellationToken = default)
    {
        var command = new RecordAuditCommand(
            Action: "ObservationCreated",
            ResourceType: "Observation",
            Actor: "api",
            ResourceId: notification.ObservationId.Value,
            PatientId: notification.PatientId.Value,
            Details: $"Loinc={notification.LoincCode.Value}");

        var result = await _sender.SendAsync(command, cancellationToken);
        if (result.Error is not null)
            _logger.LogWarning("ObservationCreatedAuditHandler: Audit failed for ObservationId={ObservationId}, Error={Error}", notification.ObservationId.Value, result.Error);
    }
}
