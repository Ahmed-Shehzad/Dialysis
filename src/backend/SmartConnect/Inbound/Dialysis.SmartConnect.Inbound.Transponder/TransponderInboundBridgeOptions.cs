namespace Dialysis.SmartConnect.Inbound.Transponder;

/// <summary>Bind <c>SmartConnect:TransponderInbound</c>. Requires <see cref="DefaultFlowId"/> and a registered <c>ITransponderTransport</c>.</summary>
public sealed class TransponderInboundBridgeOptions
{
    /// <summary>All broker deliveries are dispatched to this flow.</summary>
    public Guid DefaultFlowId { get; set; }

    /// <summary>When true, map JSON content types to <see cref="Dialysis.SmartConnect.PayloadFormat.Json"/>.</summary>
    public bool TreatJsonContentTypeAsJson { get; set; } = true;
}
