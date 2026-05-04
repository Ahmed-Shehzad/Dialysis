namespace Dialysis.BuildingBlocks.Transponder.Transport.ServerSentEvents;

/// <summary>HTTP paths for the SSE ingress relay (see <see cref="TransponderSseServerExtensions"/>).</summary>
public sealed class TransponderSseIngressOptions
{
    /// <summary>URL prefix without trailing slash (default <c>/transponder/sse</c>). POST <c>{PathPrefix}/publish</c>, GET <c>{PathPrefix}/subscribe</c>.</summary>
    public string PathPrefix { get; set; } = "/transponder/sse";
}
