namespace Dialysis.SmartConnect.Inbound;

/// <summary>
/// Outcome of an inbound accept + <see cref="IFlowRuntime.DispatchAsync"/> call, with a suggested HTTP status for gateway mappings.
/// </summary>
public sealed class InboundReceiveResult
{
    public required bool Succeeded { get; init; }

    public string? Error { get; init; }

    public IReadOnlyList<int> OutboundRoutesAttempted { get; init; } = [];

    /// <summary>HTTP status code hint for API layers (e.g. 200, 400, 404, 409, 500).</summary>
    public int SuggestedHttpStatus { get; init; } = 200;
}
