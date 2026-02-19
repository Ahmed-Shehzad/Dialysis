using BuildingBlocks.Tenancy;
using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Alarm.Application.Features.GetAlarms;

internal sealed class GetAlarmsQueryHandler : IQueryHandler<GetAlarmsQuery, GetAlarmsResponse>
{
    private readonly IAlarmReadStore _readStore;
    private readonly ITenantContext _tenant;

    public GetAlarmsQueryHandler(IAlarmReadStore readStore, ITenantContext tenant)
    {
        _readStore = readStore;
        _tenant = tenant;
    }

    public async Task<GetAlarmsResponse> HandleAsync(GetAlarmsQuery request, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.Id))
        {
            AlarmReadDto? single = await _readStore.GetByIdAsync(_tenant.TenantId, request.Id, cancellationToken);
            List<AlarmDto> singleDtos = single is not null ? [ToDto(single)] : [];
            return new GetAlarmsResponse(singleDtos);
        }

        DeviceId? deviceId = string.IsNullOrWhiteSpace(request.DeviceId) ? null : new DeviceId(request.DeviceId);
        IReadOnlyList<AlarmReadDto> alarms = await _readStore.GetAlarmsAsync(
            _tenant.TenantId,
            deviceId,
            request.SessionId,
            request.FromUtc,
            request.ToUtc,
            cancellationToken);

        var dtos = alarms.Select(ToDto).ToList();
        return new GetAlarmsResponse(dtos);
    }

    private static AlarmDto ToDto(AlarmReadDto r) =>
        new(r.Id, r.AlarmType, r.SourceCode, r.SourceLimits, r.Priority, r.InterpretationType, r.Abnormality, r.EventPhase, r.AlarmState, r.ActivityState, r.DeviceId, r.SessionId, r.OccurredAt);
}
