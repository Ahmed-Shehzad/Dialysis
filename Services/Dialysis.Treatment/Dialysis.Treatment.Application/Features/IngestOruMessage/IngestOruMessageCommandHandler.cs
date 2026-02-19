using BuildingBlocks.TimeSync;

using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Features.RecordObservation;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.Treatment.Application.Features.IngestOruMessage;

internal sealed class IngestOruMessageCommandHandler : ICommandHandler<IngestOruMessageCommand, IngestOruMessageResponse>
{
    private readonly ISender _sender;
    private readonly IOruMessageParser _parser;
    private readonly IDeviceRegistrationClient _deviceRegistration;
    private readonly TimeSyncOptions _timeSync;
    private readonly ILogger<IngestOruMessageCommandHandler> _logger;

    public IngestOruMessageCommandHandler(ISender sender, IOruMessageParser parser, IDeviceRegistrationClient deviceRegistration, IOptions<TimeSyncOptions> timeSync, ILogger<IngestOruMessageCommandHandler> logger)
    {
        _sender = sender;
        _parser = parser;
        _deviceRegistration = deviceRegistration;
        _timeSync = timeSync.Value;
        _logger = logger;
    }

    public async Task<IngestOruMessageResponse> HandleAsync(IngestOruMessageCommand request, CancellationToken cancellationToken = default)
    {
        OruParseResult parseResult = _parser.Parse(request.RawHl7Message);

        CheckTimestampDrift(parseResult.MessageTimestamp);

        if (!string.IsNullOrWhiteSpace(parseResult.DeviceEui64))
            await _deviceRegistration.EnsureRegisteredAsync(parseResult.DeviceEui64, cancellationToken);

        if (parseResult.Observations.Count == 0)
            return new IngestOruMessageResponse(parseResult.SessionId, 0, true);

        var recordCommand = new RecordObservationCommand(
            parseResult.SessionId,
            parseResult.PatientMrn,
            parseResult.DeviceId,
            parseResult.Phase,
            parseResult.Observations,
            parseResult.DeviceEui64,
            parseResult.TherapyId);

        RecordObservationResponse response = await _sender.SendAsync(recordCommand, cancellationToken);
        return new IngestOruMessageResponse(response.SessionId, response.ObservationCount, true);
    }

    private void CheckTimestampDrift(DateTimeOffset? messageTimestamp)
    {
        if (_timeSync.MaxAllowedDriftSeconds <= 0 || !_timeSync.LogDriftWarnings) return;

        double? drift = Hl7TimeSyncHelper.GetDriftSeconds(messageTimestamp);
        if (drift.HasValue && drift.Value > _timeSync.MaxAllowedDriftSeconds)
            _logger.LogWarning(
                "HL7 ORU^R01 message timestamp drift exceeds threshold. MessageTime={MessageTime}, DriftSeconds={Drift:F0}, MaxAllowed={MaxAllowed}",
                messageTimestamp, drift.Value, _timeSync.MaxAllowedDriftSeconds);
    }
}
