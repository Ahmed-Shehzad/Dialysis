namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>Validates pipeline plugin kinds against a <see cref="IFlowPluginRegistry"/>.</summary>
public static class PipelineValidation
{
    public static void ValidateOrThrow(IntegrationFlowPipelineDefinition pipeline, IFlowPluginRegistry registry)
    {
        foreach (var slot in pipeline.RouteFilters)
        {
            if (registry.TryResolveRouteFilter(slot.Kind) is null)
            {
                throw new InvalidOperationException($"Route filter kind '{slot.Kind}' is not registered.");
            }
        }

        foreach (var route in pipeline.OutboundRoutes)
        {
            if (registry.TryResolveOutboundAdapter(route.OutboundAdapterKind) is null)
            {
                throw new InvalidOperationException(
                    $"Outbound adapter kind '{route.OutboundAdapterKind}' is not registered.");
            }

            foreach (var stage in route.TransformStages)
            {
                if (registry.TryResolveTransformStage(stage.Kind) is null)
                {
                    throw new InvalidOperationException($"Transform stage kind '{stage.Kind}' is not registered.");
                }
            }

            foreach (var stage in route.ResponseTransformStages)
            {
                if (registry.TryResolveTransformStage(stage.Kind) is null)
                {
                    throw new InvalidOperationException($"Response transform stage kind '{stage.Kind}' is not registered.");
                }
            }
        }
    }
}
