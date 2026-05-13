using Dialysis.SmartConnect.Alerts;
using Dialysis.SmartConnect.Attachments;

namespace Dialysis.SmartConnect;

/// <summary>
/// Resolves pipeline <see cref="RouteFilterSlot.Kind"/>, <see cref="TransformStageSlot.Kind"/>,
/// <see cref="OutboundRouteSlot.OutboundAdapterKind"/>, <see cref="AttachmentHandlerSlot.Kind"/>,
/// and <see cref="AlertActionSlot.Kind"/> strings to runtime components.
/// </summary>
public interface IFlowPluginRegistry
{
    IRouteFilter? TryResolveRouteFilter(string kind);

    ITransformStage? TryResolveTransformStage(string kind);

    IOutboundAdapter? TryResolveOutboundAdapter(string kind);

    IAttachmentHandler? TryResolveAttachmentHandler(string kind);

    IAlertActionProvider? TryResolveAlertActionProvider(string kind);
}
