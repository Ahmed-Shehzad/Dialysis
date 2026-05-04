namespace Dialysis.SmartConnect;

/// <summary>
/// Deployable integration pipeline: inbound acceptance, route filters, outbound routes with transforms.
/// </summary>
public sealed class IntegrationFlow
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public FlowRuntimeState RuntimeState { get; init; }

    public required IntegrationFlowPipelineDefinition Pipeline { get; init; }
}
