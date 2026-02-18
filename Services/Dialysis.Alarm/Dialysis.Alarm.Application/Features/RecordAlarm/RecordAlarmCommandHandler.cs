using Dialysis.Alarm.Application.Abstractions;

using Intercessor.Abstractions;

using AlarmDomain = Dialysis.Alarm.Application.Domain.Alarm;

namespace Dialysis.Alarm.Application.Features.RecordAlarm;

internal sealed class RecordAlarmCommandHandler : ICommandHandler<RecordAlarmCommand, RecordAlarmResponse>
{
    private readonly IAlarmRepository _repository;

    public RecordAlarmCommandHandler(IAlarmRepository repository)
    {
        _repository = repository;
    }

    public async Task<RecordAlarmResponse> HandleAsync(RecordAlarmCommand request, CancellationToken cancellationToken = default)
    {
        var alarm = AlarmDomain.Raise(request.Alarm);

        await _repository.AddAsync(alarm, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return new RecordAlarmResponse(alarm.Id.ToString());
    }
}
