using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Application.Features.RecordAlarm;

using Intercessor.Abstractions;

namespace Dialysis.Alarm.Application.Features.IngestOruR40Message;

internal sealed class IngestOruR40MessageCommandHandler : ICommandHandler<IngestOruR40MessageCommand, IngestOruR40MessageResponse>
{
    private readonly ISender _sender;
    private readonly IOruR40MessageParser _parser;

    public IngestOruR40MessageCommandHandler(ISender sender, IOruR40MessageParser parser)
    {
        _sender = sender;
        _parser = parser;
    }

    public async Task<IngestOruR40MessageResponse> HandleAsync(IngestOruR40MessageCommand request, CancellationToken cancellationToken = default)
    {
        OruR40ParseResult parseResult = _parser.Parse(request.RawHl7Message);
        var alarmIds = new List<string>();

        foreach (AlarmInfo alarm in parseResult.Alarms)
        {
            var command = new RecordAlarmCommand(alarm);
            RecordAlarmResponse response = await _sender.SendAsync(command, cancellationToken);
            alarmIds.Add(response.AlarmId);
        }

        return new IngestOruR40MessageResponse(alarmIds.Count, alarmIds);
    }
}
