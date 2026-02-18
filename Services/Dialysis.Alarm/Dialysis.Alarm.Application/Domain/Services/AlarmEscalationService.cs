using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Domain.ValueObjects;

using Microsoft.Extensions.Logging;

namespace Dialysis.Alarm.Application.Domain.Services;

public sealed class AlarmEscalationService
{
    private static readonly TimeSpan EscalationWindow = TimeSpan.FromMinutes(5);
    private static readonly int EscalationThreshold = 3;

    private readonly ILogger<AlarmEscalationService> _logger;

    public AlarmEscalationService(ILogger<AlarmEscalationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public EscalationResult Evaluate(IReadOnlyList<Domain.Alarm> recentAlarms, DeviceId? deviceId)
    {
        var activeAlarms = recentAlarms
            .Where(a => a.AlarmState != AlarmState.Cleared && a.AlarmState != AlarmState.Acknowledged)
            .Where(a => a.OccurredAt >= DateTimeOffset.UtcNow - EscalationWindow)
            .ToList();

        if (activeAlarms.Count >= EscalationThreshold)
        {
            _logger.LogWarning(
                "Alarm escalation triggered for device {DeviceId}: {Count} active alarms within {Window} minutes",
                deviceId,
                activeAlarms.Count,
                EscalationWindow.TotalMinutes);

            return new EscalationResult(
                ShouldEscalate: true,
                ActiveAlarmCount: activeAlarms.Count,
                DeviceId: deviceId,
                Reason: $"{activeAlarms.Count} active alarms within {EscalationWindow.TotalMinutes} minute window");
        }

        return new EscalationResult(
            ShouldEscalate: false,
            ActiveAlarmCount: activeAlarms.Count,
            DeviceId: deviceId,
            Reason: null);
    }
}

public sealed record EscalationResult(
    bool ShouldEscalate,
    int ActiveAlarmCount,
    DeviceId? DeviceId,
    string? Reason);
