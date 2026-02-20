using BuildingBlocks.Abstractions;

using Dialysis.Alarm.Application.Events;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dialysis.Alarm.Application.Features.AlarmRaised;

/// <summary>
/// In-process consumer for AlarmEscalationTriggeredEvent. Receives events post-commit.
/// Notifies FHIR subscription dispatcher so rest-hook subscribers receive escalation Bundle (DetectedIssue).
/// </summary>
internal sealed class AlarmEscalationTriggeredEventConsumer : IIntegrationEventHandler<AlarmEscalationTriggeredEvent>
{
    private readonly IFhirSubscriptionNotifyClient _notifyClient;
    private readonly IConfiguration _config;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AlarmEscalationTriggeredEventConsumer> _logger;

    public AlarmEscalationTriggeredEventConsumer(
        IFhirSubscriptionNotifyClient notifyClient,
        IConfiguration config,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AlarmEscalationTriggeredEventConsumer> logger)
    {
        _notifyClient = notifyClient;
        _config = config;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task HandleAsync(AlarmEscalationTriggeredEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "IntegrationEvent: AlarmEscalationTriggered DeviceId={DeviceId} SessionId={SessionId} ActiveCount={Count} Reason={Reason}",
            notification.DeviceId,
            notification.SessionId,
            notification.ActiveAlarmCount,
            notification.Reason);

        string? baseUrl = _config["FhirSubscription:NotifyUrl"];
        if (string.IsNullOrEmpty(baseUrl))
            return;

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(notification.DeviceId))
            parts.Add("deviceId=" + Uri.EscapeDataString(notification.DeviceId));
        if (!string.IsNullOrEmpty(notification.SessionId))
            parts.Add("sessionId=" + Uri.EscapeDataString(notification.SessionId));
        string queryString = parts.Count > 0 ? string.Join("&", parts) : "";
        string resourceUrl = baseUrl.TrimEnd('/') + "/api/alarms/fhir" + (string.IsNullOrEmpty(queryString) ? "" : "?" + queryString);
        string? auth = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();

        await _notifyClient.NotifyAsync(
            "DetectedIssue",
            resourceUrl,
            notification.TenantId,
            auth,
            cancellationToken);
    }
}
