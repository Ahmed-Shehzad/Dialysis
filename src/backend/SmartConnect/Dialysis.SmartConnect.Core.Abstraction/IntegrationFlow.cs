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

    /// <summary>User-defined labels for categorization and filtering.</summary>
    public List<string> Tags { get; init; } = [];

    /// <summary>Optional group assignment for organizational grouping.</summary>
    public Guid? GroupId { get; init; }

    /// <summary>Free-text description of the flow's purpose.</summary>
    public string? Description { get; init; }
}
