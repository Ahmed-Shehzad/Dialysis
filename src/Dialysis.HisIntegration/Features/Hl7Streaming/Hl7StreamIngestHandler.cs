using Dialysis.HisIntegration.Services;
using Intercessor.Abstractions;

namespace Dialysis.HisIntegration.Features.Hl7Streaming;

public sealed class Hl7StreamIngestHandler : ICommandHandler<Hl7StreamIngestCommand, Hl7StreamIngestResult>
{
    private readonly IHl7StreamingWriter _writer;

    public Hl7StreamIngestHandler(IHl7StreamingWriter writer)
    {
        _writer = writer;
    }

    public async Task<Hl7StreamIngestResult> HandleAsync(Hl7StreamIngestCommand request, CancellationToken cancellationToken = default)
    {
        var result = await _writer.ConvertAndPersistAsync(
            request.RawMessage,
            request.MessageType,
            request.TenantId,
            cancellationToken);

        return new Hl7StreamIngestResult
        {
            Processed = result.Processed,
            PatientId = result.PatientId,
            EncounterId = result.EncounterId,
            ResourceIds = result.ResourceIds,
            Error = result.Error
        };
    }
}
