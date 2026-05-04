namespace Dialysis.SmartConnect;

/// <summary>
/// Resolves pipeline <see cref="RouteFilterSlot.Kind"/>, <see cref="TransformStageSlot.Kind"/>,
/// and <see cref="OutboundRouteSlot.OutboundAdapterKind"/> strings to runtime components.
/// </summary>
public interface IFlowPluginRegistry
{
    IRouteFilter? TryResolveRouteFilter(string kind);

    ITransformStage? TryResolveTransformStage(string kind);

    IOutboundAdapter? TryResolveOutboundAdapter(string kind);
}
