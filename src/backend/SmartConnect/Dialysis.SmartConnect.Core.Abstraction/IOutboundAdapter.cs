namespace Dialysis.SmartConnect;

/// <summary>
/// Sends a transformed message to an external system for one outbound route.
/// </summary>
public interface IOutboundAdapter
{
    string Kind { get; }

    Task<OutboundSendResult> SendAsync(
        IntegrationMessage message,
        int outboundRouteOrdinal,
        CancellationToken cancellationToken);

    /// <summary>
    /// Slice B2: optional JSON Schema (Draft 2020-12) describing the shape of this
    /// adapter's per-route parameters JSON. The Management API surfaces it at
    /// <c>GET /smartconnect/v1/admin/connectors/outbound/{kind}/schema</c> so the
    /// operator-shell + channel editor can render a form-driven config UI per adapter
    /// kind instead of forcing operators to edit raw JSON. Returns <c>null</c> for
    /// adapters that haven't published a schema yet — those keep accepting any JSON the
    /// runtime can deserialise.
    /// </summary>
    string? GetParametersSchema() => null;
}

public readonly record struct OutboundSendResult(bool Succeeded, string? ErrorDetail, byte[]? ResponsePayload = null);
