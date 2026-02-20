using BuildingBlocks.Abstractions;

using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Events;

using Microsoft.Extensions.Logging;

namespace Dialysis.Treatment.Application.Features.ThresholdBreachDetected;

/// <summary>
/// In-process consumer for ThresholdBreachDetectedIntegrationEvent. Receives events post-commit.
/// When AlarmApi:BaseUrl is configured, creates an alarm in the Alarm context for FHIR DetectedIssue.
/// </summary>
internal sealed class ThresholdBreachDetectedIntegrationEventConsumer : IIntegrationEventHandler<ThresholdBreachDetectedIntegrationEvent>
{
    private readonly IAlarmApiClient _alarmApiClient;
    private readonly ILogger<ThresholdBreachDetectedIntegrationEventConsumer> _logger;

    public ThresholdBreachDetectedIntegrationEventConsumer(
        IAlarmApiClient alarmApiClient,
        ILogger<ThresholdBreachDetectedIntegrationEventConsumer> logger)
    {
        _alarmApiClient = alarmApiClient;
        _logger = logger;
    }

    public async Task HandleAsync(ThresholdBreachDetectedIntegrationEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "IntegrationEvent: ThresholdBreachDetected SessionId={SessionId} Code={Code} BreachType={BreachType}",
            notification.SessionId,
            notification.Code,
            notification.BreachType);

        _ = await _alarmApiClient.RecordFromThresholdBreachAsync(
            notification.SessionId,
            notification.DeviceId,
            notification.BreachType,
            notification.Code,
            notification.ObservedValue,
            notification.ThresholdValue,
            notification.Direction,
            notification.TreatmentSessionId.ToString(),
            notification.ObservationId.ToString(),
            notification.TenantId,
            cancellationToken);
    }
}
