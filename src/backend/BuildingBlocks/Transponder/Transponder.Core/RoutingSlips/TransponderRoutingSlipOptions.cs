namespace Dialysis.BuildingBlocks.Transponder.RoutingSlips;

/// <summary>Collects routing slip activity registrations for DI.</summary>
public sealed class TransponderRoutingSlipOptions
{
    public Dictionary<string, Type> ActivitiesByName { get; } = new(StringComparer.Ordinal);
}
