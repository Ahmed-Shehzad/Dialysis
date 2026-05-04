namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Record of a successfully executed itinerary step, used for reverse compensation on fault.
/// </summary>
public sealed class TransponderRoutingSlipCompletedActivityEntry
{
    public int Index { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ArgumentsJson { get; set; }
}
