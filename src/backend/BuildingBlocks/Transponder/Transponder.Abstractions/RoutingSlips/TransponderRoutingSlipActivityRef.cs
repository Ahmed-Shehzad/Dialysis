namespace Dialysis.BuildingBlocks.Transponder.RoutingSlips;

/// <summary>
/// One step in a routing slip itinerary: a registered activity <see cref="Name"/> plus optional JSON arguments.
/// </summary>
public sealed class TransponderRoutingSlipActivityRef
{
    /// <summary>Must match a registered routing slip activity name (not <c>required</c> so saga JSON round-trips with System.Text.Json).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional JSON payload passed to the activity (interpreted by the activity implementation).</summary>
    public string? ArgumentsJson { get; set; }
}
