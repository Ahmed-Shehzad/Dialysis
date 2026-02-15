using Dialysis.HisIntegration.Services;
using Intercessor.Abstractions;

namespace Dialysis.HisIntegration.Features.AdtSync;

public sealed class AdtIngestHandler : ICommandHandler<AdtIngestCommand, AdtIngestResult>
{
    private readonly AdtMessageParser _parser;
    private readonly IFhirAdtWriter _writer;
    private readonly IProvenanceRecorder _provenance;

    public AdtIngestHandler(AdtMessageParser parser, IFhirAdtWriter writer, IProvenanceRecorder provenance)
    {
        _parser = parser;
        _writer = writer;
        _provenance = provenance;
    }

    public async Task<AdtIngestResult> HandleAsync(AdtIngestCommand request, CancellationToken cancellationToken = default)
    {
        var data = _parser.Parse(request.RawMessage);
        if (data is null)
        {
            return new AdtIngestResult { Processed = false, Message = "Invalid ADT message" };
        }

        var (patientId, encounterId) = await _writer.WriteAdtAsync(data, cancellationToken);
        if (patientId is null && encounterId is null)
        {
            return new AdtIngestResult { Processed = true, Message = "Parsed; FHIR write skipped (no BaseUrl or MRN)" };
        }

        if (!string.IsNullOrEmpty(patientId))
            await _provenance.RecordAdtProvenanceAsync(patientId, "Patient", data.MessageType, data, cancellationToken);
        if (!string.IsNullOrEmpty(encounterId))
            await _provenance.RecordAdtProvenanceAsync(encounterId, "Encounter", data.MessageType, data, cancellationToken);

        return new AdtIngestResult { Processed = true, PatientId = patientId, EncounterId = encounterId };
    }
}
