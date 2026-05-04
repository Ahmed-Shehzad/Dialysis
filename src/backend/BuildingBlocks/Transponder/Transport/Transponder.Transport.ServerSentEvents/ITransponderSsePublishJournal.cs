using Microsoft.AspNetCore.Http;

namespace Dialysis.BuildingBlocks.Transponder.Transport.ServerSentEvents;

/// <summary>Optional durability hook before fan-out to all open SSE streams.</summary>
public interface ITransponderSsePublishJournal
{
    ValueTask AppendAsync(
        TransponderSseEnvelopeDto envelope,
        HttpContext context,
        CancellationToken cancellationToken = default);
}
