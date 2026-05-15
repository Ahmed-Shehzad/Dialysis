using System.Text.Json;
using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

/// <summary>
/// EF Core <see cref="ICodeTemplateLibraryRepository"/>. Reads + writes <see cref="CodeTemplateLibraryEntity"/>
/// and its <see cref="CodeTemplateEntity"/> children, plus joins against <see cref="IntegrationFlowEntity"/>
/// when resolving "linked templates for a flow" (union of library.LinkedFlowIds and flow.LinkedLibraryIds).
/// </summary>
public sealed class EfCodeTemplateLibraryRepository(SmartConnectDbContext db) : ICodeTemplateLibraryRepository
{
    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNamingPolicy = null };

    public async Task<IReadOnlyList<CodeTemplateLibrary>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var libs = await db.CodeTemplateLibraries.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        if (libs.Count == 0)
            return [];
        var libIds = libs.Select(l => l.Id).ToList();
        var templates = await db.CodeTemplates.AsNoTracking()
            .Where(t => libIds.Contains(t.LibraryId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var grouped = templates.GroupBy(t => t.LibraryId).ToDictionary(g => g.Key, g => [.. g.OrderBy(t => t.Position)]);
        return [.. libs.Select(l => ToDomain(l, grouped.TryGetValue(l.Id, out var ts) ? ts : []))];
    }

    public async Task<CodeTemplateLibrary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lib = await db.CodeTemplateLibraries.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken).ConfigureAwait(false);
        if (lib is null)
            return null;
        var templates = await db.CodeTemplates.AsNoTracking()
            .Where(t => t.LibraryId == id)
            .OrderBy(t => t.Position)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return ToDomain(lib, templates);
    }

    public async Task UpsertAsync(CodeTemplateLibrary library, CancellationToken cancellationToken = default)
    {
        var existing = await db.CodeTemplateLibraries
            .FirstOrDefaultAsync(l => l.Id == library.Id, cancellationToken).ConfigureAwait(false);
        var linkedFlowIdsJson = JsonSerializer.Serialize(library.LinkedFlowIds, _jsonOpts);

        if (existing is null)
        {
            db.CodeTemplateLibraries.Add(new CodeTemplateLibraryEntity
            {
                Id = library.Id,
                Name = library.Name,
                Description = library.Description,
                LinkedFlowIdsJson = linkedFlowIdsJson,
                AutoLinkNewFlows = library.AutoLinkNewFlows,
                Revision = library.Revision,
                LastModifiedUtc = library.LastModifiedUtc == default ? DateTimeOffset.UtcNow : library.LastModifiedUtc,
            });
        }
        else
        {
            existing.Name = library.Name;
            existing.Description = library.Description;
            existing.LinkedFlowIdsJson = linkedFlowIdsJson;
            existing.AutoLinkNewFlows = library.AutoLinkNewFlows;
            existing.Revision = library.Revision;
            existing.LastModifiedUtc = library.LastModifiedUtc == default ? DateTimeOffset.UtcNow : library.LastModifiedUtc;
        }

        // Replace template set: simple delete-then-insert keeps the upsert atomic and avoids per-template diffing.
        var oldTemplates = await db.CodeTemplates
            .Where(t => t.LibraryId == library.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        db.CodeTemplates.RemoveRange(oldTemplates);

        var position = 0;
        foreach (var t in library.Templates)
        {
            db.CodeTemplates.Add(new CodeTemplateEntity
            {
                Id = t.Id == Guid.Empty ? Guid.CreateVersion7() : t.Id,
                LibraryId = library.Id,
                Name = t.Name,
                Code = t.Code,
                Type = (int)t.Type,
                ContextsJson = JsonSerializer.Serialize(t.Contexts.Select(c => (int)c), _jsonOpts),
                JsDoc = t.JsDoc,
                Revision = t.Revision,
                LastModifiedUtc = t.LastModifiedUtc == default ? DateTimeOffset.UtcNow : t.LastModifiedUtc,
                Position = position++,
            });
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await db.CodeTemplateLibraries
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
            return;
        db.CodeTemplateLibraries.Remove(existing);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CodeTemplate>> GetLinkedTemplatesForFlowAsync(
        Guid flowId,
        CodeTemplateContext context,
        CancellationToken cancellationToken = default)
    {
        // Side A: libraries whose LinkedFlowIds contains flowId.
        // Side B: libraries whose Id is in this flow's PipelineDefinition.LinkedLibraryIds.
        var libraries = await db.CodeTemplateLibraries.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        if (libraries.Count == 0)
            return [];

        var flowEntity = await db.IntegrationFlows.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == flowId, cancellationToken).ConfigureAwait(false);
        var flowLinkedLibIds = new HashSet<Guid>();
        if (flowEntity is not null && !string.IsNullOrWhiteSpace(flowEntity.PipelineJson))
        {
            try
            {
                var pipeline = PipelineJsonSerializer.Deserialize(flowEntity.PipelineJson);
                foreach (var id in pipeline.LinkedLibraryIds)
                    flowLinkedLibIds.Add(id);
            }
            catch (JsonException)
            {
                // Tolerate malformed pipeline JSON.
            }
        }

        var matchedLibraryIds = new HashSet<Guid>();
        foreach (var lib in libraries)
        {
            if (flowLinkedLibIds.Contains(lib.Id))
            {
                matchedLibraryIds.Add(lib.Id);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(lib.LinkedFlowIdsJson))
            {
                try
                {
                    var ids = JsonSerializer.Deserialize<List<Guid>>(lib.LinkedFlowIdsJson, _jsonOpts) ?? [];
                    if (ids.Contains(flowId))
                        matchedLibraryIds.Add(lib.Id);
                }
                catch (JsonException)
                {
                    // Skip malformed linkage column on this library.
                }
            }
        }

        if (matchedLibraryIds.Count == 0)
            return [];

        var templates = await db.CodeTemplates.AsNoTracking()
            .Where(t => matchedLibraryIds.Contains(t.LibraryId))
            .OrderBy(t => t.Position)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var ctxValue = (int)context;
        var result = new List<CodeTemplate>(templates.Count);
        foreach (var t in templates)
        {
            if (TemplateMatchesContext(t.ContextsJson, ctxValue))
            {
                result.Add(ToDomain(t));
            }
        }
        return result;
    }

    private static bool TemplateMatchesContext(string contextsJson, int context)
    {
        try
        {
            var contexts = JsonSerializer.Deserialize<List<int>>(contextsJson, _jsonOpts);
            return contexts is { } cs && cs.Contains(context);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static CodeTemplateLibrary ToDomain(CodeTemplateLibraryEntity lib, IReadOnlyList<CodeTemplateEntity> templates)
    {
        var linkedFlows = new List<Guid>();
        if (!string.IsNullOrWhiteSpace(lib.LinkedFlowIdsJson))
        {
            try
            { linkedFlows = JsonSerializer.Deserialize<List<Guid>>(lib.LinkedFlowIdsJson, _jsonOpts) ?? []; }
            catch (JsonException) { /* tolerate */ }
        }

        return new CodeTemplateLibrary
        {
            Id = lib.Id,
            Name = lib.Name,
            Description = lib.Description,
            LinkedFlowIds = linkedFlows,
            AutoLinkNewFlows = lib.AutoLinkNewFlows,
            Revision = lib.Revision,
            LastModifiedUtc = lib.LastModifiedUtc,
            Templates = [.. templates.Select(ToDomain)],
        };
    }

    private static CodeTemplate ToDomain(CodeTemplateEntity t)
    {
        var contexts = new List<CodeTemplateContext>();
        if (!string.IsNullOrWhiteSpace(t.ContextsJson))
        {
            try
            {
                var ids = JsonSerializer.Deserialize<List<int>>(t.ContextsJson, _jsonOpts) ?? [];
                foreach (var i in ids)
                    contexts.Add((CodeTemplateContext)i);
            }
            catch (JsonException) { /* tolerate */ }
        }

        return new CodeTemplate
        {
            Id = t.Id,
            LibraryId = t.LibraryId,
            Name = t.Name,
            Code = t.Code,
            Type = (CodeTemplateType)t.Type,
            Contexts = contexts,
            JsDoc = t.JsDoc,
            Revision = t.Revision,
            LastModifiedUtc = t.LastModifiedUtc,
            Position = t.Position,
        };
    }
}
