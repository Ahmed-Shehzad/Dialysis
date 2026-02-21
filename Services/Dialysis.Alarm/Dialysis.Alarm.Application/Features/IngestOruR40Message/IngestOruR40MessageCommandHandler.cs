using BuildingBlocks.TimeSync;

using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Application.Domain;
using Dialysis.Alarm.Application.Features.RecordAlarm;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.Alarm.Application.Features.IngestOruR40Message;

public sealed class IngestOruR40MessageCommandHandler : ICommandHandler<IngestOruR40MessageCommand, IngestOruR40MessageResponse>
{
    private readonly ISender _sender;
    private readonly IOruR40MessageParser _parser;
    private readonly IDeviceRegistrationClient _deviceRegistration;
    private readonly TimeSyncOptions _timeSync;
    private readonly ILogger<IngestOruR40MessageCommandHandler> _logger;

    public IngestOruR40MessageCommandHandler(ISender sender, IOruR40MessageParser parser, IDeviceRegistrationClient deviceRegistration, IOptions<TimeSyncOptions> timeSync, ILogger<IngestOruR40MessageCommandHandler> logger)
    {
        _sender = sender;
        _parser = parser;
        _deviceRegistration = deviceRegistration;
        _timeSync = timeSync.Value;
        _logger = logger;
    }

    public async Task<IngestOruR40MessageResponse> HandleAsync(IngestOruR40MessageCommand request, CancellationToken cancellationToken = default)
    {
        DateTimeOffset? messageTimestamp = Hl7TimeSyncHelper.ExtractMessageTimestamp(request.RawHl7Message);
        double? driftSeconds = null;
        if (_timeSync.MaxAllowedDriftSeconds > 0)
            driftSeconds = Hl7TimeSyncHelper.GetDriftSeconds(messageTimestamp);
        CheckTimestampDrift(messageTimestamp);

        OruR40ParseResult parseResult = _parser.Parse(request.RawHl7Message);
        var alarmIds = new List<string>();

        if (!string.IsNullOrWhiteSpace(parseResult.DeviceId))
            await _deviceRegistration.EnsureRegisteredAsync(parseResult.DeviceId, cancellationToken);

        foreach (AlarmInfo alarm in parseResult.Alarms)
        {
            AlarmInfo alarmWithDrift = alarm with { MessageTimeDriftSeconds = driftSeconds };
            var command = new RecordAlarmCommand(alarmWithDrift);
            RecordAlarmResponse response = await _sender.SendAsync(command, cancellationToken);
            alarmIds.Add(response.AlarmId);
        }

        return new IngestOruR40MessageResponse(alarmIds.Count, alarmIds);
    }

    private void CheckTimestampDrift(DateTimeOffset? messageTimestamp)
    {
        if (_timeSync.MaxAllowedDriftSeconds <= 0 || !_timeSync.LogDriftWarnings) return;

        double? drift = Hl7TimeSyncHelper.GetDriftSeconds(messageTimestamp);
        if (drift.HasValue && drift.Value > _timeSync.MaxAllowedDriftSeconds)
            _logger.LogWarning(
                "HL7 ORU^R40 message timestamp drift exceeds threshold. MessageTime={MessageTime}, DriftSeconds={Drift:F0}, MaxAllowed={MaxAllowed}",
                messageTimestamp, drift.Value, _timeSync.MaxAllowedDriftSeconds);
    }
}
