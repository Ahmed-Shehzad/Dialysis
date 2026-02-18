using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Features.RecordObservation;

using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.IngestOruMessage;

internal sealed class IngestOruMessageCommandHandler : ICommandHandler<IngestOruMessageCommand, IngestOruMessageResponse>
{
    private readonly ISender _sender;
    private readonly IOruMessageParser _parser;

    public IngestOruMessageCommandHandler(ISender sender, IOruMessageParser parser)
    {
        _sender = sender;
        _parser = parser;
    }

    public async Task<IngestOruMessageResponse> HandleAsync(IngestOruMessageCommand request, CancellationToken cancellationToken = default)
    {
        OruParseResult parseResult = _parser.Parse(request.RawHl7Message);

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
}
