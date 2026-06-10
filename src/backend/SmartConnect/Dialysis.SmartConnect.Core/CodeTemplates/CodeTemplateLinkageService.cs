using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

namespace Dialysis.SmartConnect.CodeTemplates;

/// <summary>
/// Maintains denormalized linkage consistency between <see cref="CodeTemplateLibrary.LinkedFlowIds"/> and
/// <see cref="IntegrationFlowPipelineDefinition.LinkedLibraryIds"/>. Reconciles in both directions on write
/// so reads from either side surface the same union.
/// </summary>
public sealed class CodeTemplateLinkageService
{
    private readonly ICodeTemplateLibraryRepository _libraries;
    private readonly IIntegrationFlowRepository _flows;
    /// <summary>
    /// Maintains denormalized linkage consistency between <see cref="CodeTemplateLibrary.LinkedFlowIds"/> and
    /// <see cref="IntegrationFlowPipelineDefinition.LinkedLibraryIds"/>. Reconciles in both directions on write
    /// so reads from either side surface the same union.
    /// </summary>
    public CodeTemplateLinkageService(ICodeTemplateLibraryRepository libraries,
        IIntegrationFlowRepository flows)
    {
        _libraries = libraries;
        _flows = flows;
    }
    /// <summary>
    /// Called after a library is saved with <paramref name="newLinkedFlowIds"/>. Adds the library Id to every
    /// flow's <c>LinkedLibraryIds</c> that newly appeared, and removes it from flows that dropped off.
    /// </summary>
    public async Task ReconcileLibraryWriteAsync(
        Guid libraryId,
        IReadOnlyList<Guid> previousLinkedFlowIds,
        IReadOnlyList<Guid> newLinkedFlowIds,
        CancellationToken cancellationToken = default)
    {
        var prev = new HashSet<Guid>(previousLinkedFlowIds);
        var next = new HashSet<Guid>(newLinkedFlowIds);
        var added = next.Except(prev);
        var removed = prev.Except(next);

        foreach (var flowId in added)
        {
            var flow = await _flows.GetByIdAsync(flowId, cancellationToken).ConfigureAwait(false);
            if (flow is null)
                continue;
            if (!flow.Pipeline.LinkedLibraryIds.Contains(libraryId))
            {
                flow.Pipeline.LinkedLibraryIds.Add(libraryId);
                await _flows.UpdateAsync(flow, cancellationToken).ConfigureAwait(false);
            }
        }

        foreach (var flowId in removed)
        {
            var flow = await _flows.GetByIdAsync(flowId, cancellationToken).ConfigureAwait(false);
            if (flow is null)
                continue;
            if (flow.Pipeline.LinkedLibraryIds.Remove(libraryId))
            {
                await _flows.UpdateAsync(flow, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Called after a flow is saved with <paramref name="newLinkedLibraryIds"/>. Mirrors the change into each
    /// library's <c>LinkedFlowIds</c>.
    /// </summary>
    public async Task ReconcileFlowWriteAsync(
        Guid flowId,
        IReadOnlyList<Guid> previousLinkedLibraryIds,
        IReadOnlyList<Guid> newLinkedLibraryIds,
        CancellationToken cancellationToken = default)
    {
        var prev = new HashSet<Guid>(previousLinkedLibraryIds);
        var next = new HashSet<Guid>(newLinkedLibraryIds);
        var added = next.Except(prev);
        var removed = prev.Except(next);

        foreach (var libraryId in added)
        {
            var lib = await _libraries.GetByIdAsync(libraryId, cancellationToken).ConfigureAwait(false);
            if (lib is null)
                continue;
            if (!lib.LinkedFlowIds.Contains(flowId))
            {
                var updated = new CodeTemplateLibrary
                {
                    Id = lib.Id,
                    Name = lib.Name,
                    Description = lib.Description,
                    LinkedFlowIds = [.. lib.LinkedFlowIds, flowId],
                    AutoLinkNewFlows = lib.AutoLinkNewFlows,
                    Revision = lib.Revision + 1,
                    LastModifiedUtc = DateTimeOffset.UtcNow,
                    Templates = lib.Templates,
                };
                await _libraries.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
            }
        }

        foreach (var libraryId in removed)
        {
            var lib = await _libraries.GetByIdAsync(libraryId, cancellationToken).ConfigureAwait(false);
            if (lib is null)
                continue;
            if (lib.LinkedFlowIds.Contains(flowId))
            {
                var newLinked = lib.LinkedFlowIds.Where(f => f != flowId).ToList();
                var updated = new CodeTemplateLibrary
                {
                    Id = lib.Id,
                    Name = lib.Name,
                    Description = lib.Description,
                    LinkedFlowIds = newLinked,
                    AutoLinkNewFlows = lib.AutoLinkNewFlows,
                    Revision = lib.Revision + 1,
                    LastModifiedUtc = DateTimeOffset.UtcNow,
                    Templates = lib.Templates,
                };
                await _libraries.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Called when a new flow is created. Auto-links the flow to every library with
    /// <see cref="CodeTemplateLibrary.AutoLinkNewFlows"/> set, on both sides.
    /// </summary>
    public async Task ApplyAutoLinkOnFlowCreateAsync(
        Guid flowId,
        CancellationToken cancellationToken = default)
    {
        var allLibs = await _libraries.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var autoLinkLibs = allLibs.Where(l => l.AutoLinkNewFlows).ToList();
        if (autoLinkLibs.Count == 0)
            return;

        var flow = await _flows.GetByIdAsync(flowId, cancellationToken).ConfigureAwait(false);
        if (flow is null)
            return;

        var added = new List<Guid>();
        foreach (var lib in autoLinkLibs)
        {
            if (!flow.Pipeline.LinkedLibraryIds.Contains(lib.Id))
            {
                flow.Pipeline.LinkedLibraryIds.Add(lib.Id);
                added.Add(lib.Id);
            }
        }

        if (added.Count > 0)
        {
            await _flows.UpdateAsync(flow, cancellationToken).ConfigureAwait(false);
            foreach (var libraryId in added)
            {
                var lib = autoLinkLibs.First(l => l.Id == libraryId);
                if (!lib.LinkedFlowIds.Contains(flowId))
                {
                    var updated = new CodeTemplateLibrary
                    {
                        Id = lib.Id,
                        Name = lib.Name,
                        Description = lib.Description,
                        LinkedFlowIds = [.. lib.LinkedFlowIds, flowId],
                        AutoLinkNewFlows = lib.AutoLinkNewFlows,
                        Revision = lib.Revision + 1,
                        LastModifiedUtc = DateTimeOffset.UtcNow,
                        Templates = lib.Templates,
                    };
                    await _libraries.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
