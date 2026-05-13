namespace Dialysis.SmartConnect.CodeTemplates;

/// <summary>
/// Named container of <see cref="CodeTemplate"/> entries, optionally linked to specific flows.
/// Linkage is denormalized: this side carries <see cref="LinkedFlowIds"/>; the flow side carries
/// <c>IntegrationFlowPipelineDefinition.LinkedLibraryIds</c>. Writes reconcile both sides.
/// </summary>
public sealed class CodeTemplateLibrary
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public IReadOnlyList<Guid> LinkedFlowIds { get; init; } = [];

    /// <summary>When true, newly-created flows are auto-linked to this library.</summary>
    public bool AutoLinkNewFlows { get; init; }

    public int Revision { get; init; } = 1;

    public DateTimeOffset LastModifiedUtc { get; init; }

    public IReadOnlyList<CodeTemplate> Templates { get; init; } = [];
}
