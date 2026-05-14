using Dialysis.SmartConnect.CodeTemplates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>
/// Maps <c>/smartconnect/v1/admin/code-template-libraries/*</c> routes for library CRUD + JSON/Mirth-XML import.
/// </summary>
public static class CodeTemplateLibraryEndpointExtensions
{
    public static IEndpointRouteBuilder MapSmartConnectCodeTemplateLibraryRoutes(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/smartconnect/v1/admin/code-template-libraries")
            .WithTags("SmartConnect Admin");

        group.MapGet("/", async (
                ICodeTemplateLibraryRepository repo,
                CancellationToken ct) =>
            {
                var all = await repo.GetAllAsync(ct).ConfigureAwait(false);
                return Results.Ok(all);
            })
            .WithName("SmartConnect_ListCodeTemplateLibraries");

        group.MapGet("/{id:guid}", async (
                Guid id,
                ICodeTemplateLibraryRepository repo,
                CancellationToken ct) =>
            {
                var lib = await repo.GetByIdAsync(id, ct).ConfigureAwait(false);
                return lib is null ? Results.NotFound() : Results.Ok(lib);
            })
            .WithName("SmartConnect_GetCodeTemplateLibrary");

        group.MapPost("/", async (
                CodeTemplateLibrary body,
                ICodeTemplateLibraryRepository repo,
                CodeTemplateLinkageService linkage,
                CancellationToken ct) =>
            {
                var id = body.Id == Guid.Empty ? Guid.CreateVersion7() : body.Id;
                var library = WithGeneratedIds(body, id);
                await repo.UpsertAsync(library, ct).ConfigureAwait(false);
                await linkage.ReconcileLibraryWriteAsync(id, [], library.LinkedFlowIds, ct).ConfigureAwait(false);
                return Results.Created($"/smartconnect/v1/admin/code-template-libraries/{id}", library);
            })
            .WithName("SmartConnect_CreateCodeTemplateLibrary");

        group.MapPut("/{id:guid}", async (
                Guid id,
                CodeTemplateLibrary body,
                ICodeTemplateLibraryRepository repo,
                CodeTemplateLinkageService linkage,
                CancellationToken ct) =>
            {
                var existing = await repo.GetByIdAsync(id, ct).ConfigureAwait(false);
                if (existing is null) return Results.NotFound();

                var library = WithGeneratedIds(body, id);
                await repo.UpsertAsync(library, ct).ConfigureAwait(false);
                await linkage.ReconcileLibraryWriteAsync(id, existing.LinkedFlowIds, library.LinkedFlowIds, ct).ConfigureAwait(false);
                return Results.Ok(library);
            })
            .WithName("SmartConnect_UpdateCodeTemplateLibrary");

        group.MapDelete("/{id:guid}", async (
                Guid id,
                ICodeTemplateLibraryRepository repo,
                CodeTemplateLinkageService linkage,
                CancellationToken ct) =>
            {
                var existing = await repo.GetByIdAsync(id, ct).ConfigureAwait(false);
                if (existing is null) return Results.NotFound();
                await linkage.ReconcileLibraryWriteAsync(id, existing.LinkedFlowIds, [], ct).ConfigureAwait(false);
                await repo.DeleteAsync(id, ct).ConfigureAwait(false);
                return Results.NoContent();
            })
            .WithName("SmartConnect_DeleteCodeTemplateLibrary");

        group.MapPost("/import", async (
                IReadOnlyList<CodeTemplateLibrary> body,
                ICodeTemplateLibraryRepository repo,
                CodeTemplateLinkageService linkage,
                CancellationToken ct) =>
            {
                var imported = new List<Guid>();
                foreach (var library in body)
                {
                    var id = library.Id == Guid.Empty ? Guid.CreateVersion7() : library.Id;
                    var withIds = WithGeneratedIds(library, id);
                    var existing = await repo.GetByIdAsync(id, ct).ConfigureAwait(false);
                    await repo.UpsertAsync(withIds, ct).ConfigureAwait(false);
                    await linkage.ReconcileLibraryWriteAsync(
                        id,
                        existing?.LinkedFlowIds ?? [],
                        withIds.LinkedFlowIds,
                        ct).ConfigureAwait(false);
                    imported.Add(id);
                }
                return Results.Ok(new { imported });
            })
            .WithName("SmartConnect_ImportCodeTemplateLibrariesJson");

        group.MapPost("/import-mirth-xml", async (
                HttpRequest request,
                MirthXmlCodeTemplateImporter importer,
                ICodeTemplateLibraryRepository repo,
                CodeTemplateLinkageService linkage,
                CancellationToken ct) =>
            {
                using var reader = new StreamReader(request.Body);
                var xml = await reader.ReadToEndAsync(ct).ConfigureAwait(false);

                IReadOnlyList<CodeTemplateLibrary> libraries;
                try
                {
                    libraries = importer.Import(xml);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }

                var imported = new List<Guid>();
                foreach (var library in libraries)
                {
                    var existing = await repo.GetByIdAsync(library.Id, ct).ConfigureAwait(false);
                    await repo.UpsertAsync(library, ct).ConfigureAwait(false);
                    await linkage.ReconcileLibraryWriteAsync(
                        library.Id,
                        existing?.LinkedFlowIds ?? [],
                        library.LinkedFlowIds,
                        ct).ConfigureAwait(false);
                    imported.Add(library.Id);
                }
                return Results.Ok(new { imported });
            })
            .WithName("SmartConnect_ImportCodeTemplateLibrariesMirthXml");

        return endpoints;
    }

    /// <summary>Fills in any missing template Ids and re-anchors them to the parent library.</summary>
    private static CodeTemplateLibrary WithGeneratedIds(CodeTemplateLibrary library, Guid libraryId)
    {
        var templates = library.Templates.Select((t, i) => new CodeTemplate
        {
            Id = t.Id == Guid.Empty ? Guid.CreateVersion7() : t.Id,
            LibraryId = libraryId,
            Name = t.Name,
            Code = t.Code,
            Type = t.Type,
            Contexts = t.Contexts,
            JsDoc = t.JsDoc,
            Revision = t.Revision <= 0 ? 1 : t.Revision,
            LastModifiedUtc = t.LastModifiedUtc == default ? DateTimeOffset.UtcNow : t.LastModifiedUtc,
            Position = i,
        }).ToList();

        return new CodeTemplateLibrary
        {
            Id = libraryId,
            Name = library.Name,
            Description = library.Description,
            LinkedFlowIds = library.LinkedFlowIds,
            AutoLinkNewFlows = library.AutoLinkNewFlows,
            Revision = library.Revision <= 0 ? 1 : library.Revision,
            LastModifiedUtc = library.LastModifiedUtc == default ? DateTimeOffset.UtcNow : library.LastModifiedUtc,
            Templates = templates,
        };
    }
}
