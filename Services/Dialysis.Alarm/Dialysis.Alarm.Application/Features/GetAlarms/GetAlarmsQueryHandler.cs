using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Alarm.Application.Features.GetAlarms;

internal sealed class GetAlarmsQueryHandler : IQueryHandler<GetAlarmsQuery, GetAlarmsResponse>
{
    private readonly IAlarmRepository _repository;

    public GetAlarmsQueryHandler(IAlarmRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetAlarmsResponse> HandleAsync(GetAlarmsQuery request, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.Id) && Ulid.TryParse(request.Id, out Ulid id))
        {
            Domain.Alarm? alarm = await _repository.GetByIdAsync(id, cancellationToken);
            List<AlarmDto> singleDtos = alarm is not null ? new List<AlarmDto> { ToDto(alarm) } : new List<AlarmDto>();
            return new GetAlarmsResponse(singleDtos);
        }

        DeviceId? deviceId = string.IsNullOrWhiteSpace(request.DeviceId) ? null : new DeviceId(request.DeviceId);
        IReadOnlyList<Domain.Alarm> alarms = await _repository.GetAlarmsAsync(
            deviceId,
            request.SessionId,
            request.FromUtc,
            request.ToUtc,
            cancellationToken);

        var dtos = alarms.Select(ToDto).ToList();
        return new GetAlarmsResponse(dtos);
    }

    private static AlarmDto ToDto(Domain.Alarm a) =>
        new(
            a.Id.ToString(),
            a.AlarmType,
            a.SourceCode,
            a.SourceLimits,
            a.Priority?.Value,
            a.InterpretationType,
            a.Abnormality,
            a.EventPhase.Value,
            a.AlarmState.Value,
            a.ActivityState.Value,
            a.DeviceId?.Value,
            a.SessionId,
            a.OccurredAt);
}
