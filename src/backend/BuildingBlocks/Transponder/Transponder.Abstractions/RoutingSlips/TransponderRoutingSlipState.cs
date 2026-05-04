namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Serializable snapshot of an in-flight routing slip (stored as saga <see cref="TransponderSagaRecord.StateJson"/>).
/// </summary>
public sealed class TransponderRoutingSlipState
{
    public List<TransponderRoutingSlipActivityRef> Itinerary { get; set; } = [];

    /// <summary>Index of the next activity to execute (0-based).</summary>
    public int CurrentIndex { get; set; }

    public Dictionary<string, string> Variables { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Successfully completed steps (in execution order), used for reverse compensation when a later step faults.</summary>
    public List<TransponderRoutingSlipCompletedActivityEntry> CompletedActivities { get; set; } = [];

    /// <summary>Optional correlation propagated on <see cref="TransponderRoutingSlipContinue"/> publishes.</summary>
    public string? CorrelationId { get; set; }
}
