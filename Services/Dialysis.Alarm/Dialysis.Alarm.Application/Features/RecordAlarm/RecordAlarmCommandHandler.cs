using BuildingBlocks.Tenancy;

using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Application.Domain.ValueObjects;

using Intercessor.Abstractions;

using AlarmDomain = Dialysis.Alarm.Application.Domain.Alarm;

namespace Dialysis.Alarm.Application.Features.RecordAlarm;

internal sealed class RecordAlarmCommandHandler : ICommandHandler<RecordAlarmCommand, RecordAlarmResponse>
{
    private readonly IAlarmRepository _repository;
    private readonly ITenantContext _tenant;

    public RecordAlarmCommandHandler(IAlarmRepository repository, ITenantContext tenant)
    {
        _repository = repository;
        _tenant = tenant;
    }

    public async Task<RecordAlarmResponse> HandleAsync(RecordAlarmCommand request, CancellationToken cancellationToken = default)
    {
        AlarmInfo info = request.Alarm;

        if (info.EventPhase.Value == EventPhase.Start.Value)
        {
            var alarm = AlarmDomain.Raise(info, _tenant.TenantId);
            await _repository.AddAsync(alarm, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            return new RecordAlarmResponse(alarm.Id.ToString());
        }

        if (info.EventPhase.Value == EventPhase.Continue.Value || info.EventPhase.Value == EventPhase.End.Value)
        {
            AlarmDomain? existing = await _repository.GetActiveBySourceAsync(info.DeviceId, info.SessionId, info.SourceCode, cancellationToken);
            if (existing is not null)
            {
                existing.UpdateState(info.EventPhase, info.AlarmState, info.ActivityState, info.OccurredAt);
                _repository.Update(existing);
                await _repository.SaveChangesAsync(cancellationToken);
                return new RecordAlarmResponse(existing.Id.ToString());
            }

            if (info.EventPhase.Value == EventPhase.Continue.Value)
            {
                var orphanAlarm = AlarmDomain.Raise(info, _tenant.TenantId);
                await _repository.AddAsync(orphanAlarm, cancellationToken);
                await _repository.SaveChangesAsync(cancellationToken);
                return new RecordAlarmResponse(orphanAlarm.Id.ToString());
            }

            return new RecordAlarmResponse(string.Empty);
        }

        var fallbackAlarm = AlarmDomain.Raise(info, _tenant.TenantId);
        await _repository.AddAsync(fallbackAlarm, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return new RecordAlarmResponse(fallbackAlarm.Id.ToString());
    }
}
