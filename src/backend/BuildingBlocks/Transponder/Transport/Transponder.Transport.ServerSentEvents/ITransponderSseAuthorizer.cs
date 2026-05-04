using Microsoft.AspNetCore.Http;

namespace Dialysis.BuildingBlocks.Transponder.Transport.ServerSentEvents;

/// <summary>Optional authorization for SSE ingress POST and GET.</summary>
public interface ITransponderSseAuthorizer
{
    ValueTask AuthorizePublishAsync(
        HttpContext context,
        TransponderSseEnvelopeDto envelope,
        CancellationToken cancellationToken = default);

    ValueTask AuthorizeSubscribeAsync(HttpContext context, CancellationToken cancellationToken = default);
}
