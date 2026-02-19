using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Application.Features.RecordAlarm;

using Intercessor.Abstractions;

namespace Dialysis.Alarm.Application.Features.IngestOruR40Message;

internal sealed class IngestOruR40MessageCommandHandler : ICommandHandler<IngestOruR40MessageCommand, IngestOruR40MessageResponse>
{
    private readonly ISender _sender;
    private readonly IOruR40MessageParser _parser;
    private readonly IDeviceRegistrationClient _deviceRegistration;

    public IngestOruR40MessageCommandHandler(ISender sender, IOruR40MessageParser parser, IDeviceRegistrationClient deviceRegistration)
    {
        _sender = sender;
        _parser = parser;
        _deviceRegistration = deviceRegistration;
    }

    public async Task<IngestOruR40MessageResponse> HandleAsync(IngestOruR40MessageCommand request, CancellationToken cancellationToken = default)
    {
        OruR40ParseResult parseResult = _parser.Parse(request.RawHl7Message);
        var alarmIds = new List<string>();

        if (!string.IsNullOrWhiteSpace(parseResult.DeviceId))
            await _deviceRegistration.EnsureRegisteredAsync(parseResult.DeviceId, cancellationToken);

        foreach (AlarmInfo alarm in parseResult.Alarms)
        {
            var command = new RecordAlarmCommand(alarm);
            RecordAlarmResponse response = await _sender.SendAsync(command, cancellationToken);
            alarmIds.Add(response.AlarmId);
        }

        return new IngestOruR40MessageResponse(alarmIds.Count, alarmIds);
    }
}
