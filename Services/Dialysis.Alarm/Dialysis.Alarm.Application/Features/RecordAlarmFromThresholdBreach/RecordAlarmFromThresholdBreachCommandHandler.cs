using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Domain;
using Dialysis.Alarm.Application.Domain.ValueObjects;
using Dialysis.Alarm.Application.Features.RecordAlarm;

using Intercessor.Abstractions;

using SessionId = BuildingBlocks.ValueObjects.SessionId;

namespace Dialysis.Alarm.Application.Features.RecordAlarmFromThresholdBreach;

internal sealed class RecordAlarmFromThresholdBreachCommandHandler : ICommandHandler<RecordAlarmFromThresholdBreachCommand, RecordAlarmFromThresholdBreachResponse>
{
    private readonly ISender _sender;

    public RecordAlarmFromThresholdBreachCommandHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task<RecordAlarmFromThresholdBreachResponse> HandleAsync(RecordAlarmFromThresholdBreachCommand request, CancellationToken cancellationToken = default)
    {
        DeviceId? deviceId = string.IsNullOrWhiteSpace(request.DeviceId) ? null : new DeviceId(request.DeviceId);
        SessionId? sessionId = string.IsNullOrWhiteSpace(request.SessionId) ? null : new SessionId(request.SessionId);

        var createParams = new AlarmCreateParams(
            AlarmType: request.BreachType,
            SourceCode: request.Code,
            SourceLimits: $"{request.ThresholdValue} ({request.Direction})",
            State: new AlarmStateDescriptor(EventPhase.Start, AlarmState.Active, ActivityState.Enabled),
            Priority: AlarmPriority.Medium,
            InterpretationType: "SP",
            Abnormality: request.Direction == "below" ? "L" : "H",
            DisplayName: $"{request.BreachType}: {request.Code}",
            DeviceId: deviceId,
            SessionId: sessionId,
            OccurredAt: DateTimeOffset.UtcNow);

        AlarmInfo info = AlarmInfo.Create(createParams);
        RecordAlarmResponse recordResponse = await _sender.SendAsync(new RecordAlarmCommand(info), cancellationToken);
        return new RecordAlarmFromThresholdBreachResponse(recordResponse.AlarmId);
    }
}
