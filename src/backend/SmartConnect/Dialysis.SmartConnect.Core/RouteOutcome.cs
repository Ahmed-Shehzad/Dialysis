namespace Dialysis.SmartConnect;

/// <summary>
/// Per-route execution result reduced by the outbound loop. Captures whether the route was
/// attempted (DSF did not skip it), whether it failed, and the response payload (if any) for
/// the lowest-ordinal-first selection rule.
/// </summary>
internal sealed record RouteOutcome
{
    /// <summary>
    /// Per-route execution result reduced by the outbound loop. Captures whether the route was
    /// attempted (DSF did not skip it), whether it failed, and the response payload (if any) for
    /// the lowest-ordinal-first selection rule.
    /// </summary>
    public RouteOutcome(int Ordinal, bool Attempted, bool Failed, string RouteName, byte[]? ResponsePayload)
    {
        this.Ordinal = Ordinal;
        this.Attempted = Attempted;
        this.Failed = Failed;
        this.RouteName = RouteName;
        this.ResponsePayload = ResponsePayload;
    }
    public int Ordinal { get; init; }
    public bool Attempted { get; init; }
    public bool Failed { get; init; }
    public string RouteName { get; init; }
    public byte[]? ResponsePayload { get; init; }
    public void Deconstruct(out int Ordinal, out bool Attempted, out bool Failed, out string RouteName, out byte[]? ResponsePayload)
    {
        Ordinal = this.Ordinal;
        Attempted = this.Attempted;
        Failed = this.Failed;
        RouteName = this.RouteName;
        ResponsePayload = this.ResponsePayload;
    }
}
