using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class CodeTemplateLibraryRepositoryTests
{
    [Fact]
    public async Task Upsert_and_get_round_trip_preserves_templates_and_link_metadata()
    {
        await using var sp = BuildServices();
        var repo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();

        var libraryId = Guid.CreateVersion7();
        var flowId = Guid.CreateVersion7();
        var library = NewLibrary(libraryId, linkedFlowIds: [flowId], templates:
            [
                NewTemplate(libraryId, "helpA", "function helpA(){ return 'A'; }", [CodeTemplateContext.SourceTransformer]),
                NewTemplate(libraryId, "helpB", "function helpB(){ return 'B'; }", [CodeTemplateContext.DestinationTransformer]),
            ]);

        await repo.UpsertAsync(library, CancellationToken.None);
        var fetched = await repo.GetByIdAsync(libraryId, CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.Equal("My lib", fetched!.Name);
        Assert.Contains(flowId, fetched.LinkedFlowIds);
        Assert.Equal(2, fetched.Templates.Count);
        Assert.Equal("helpA", fetched.Templates[0].Name);
        Assert.Equal(CodeTemplateContext.SourceTransformer, fetched.Templates[0].Contexts[0]);
    }

    [Fact]
    public async Task Delete_cascades_templates()
    {
        await using var sp = BuildServices();
        var repo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();
        var db = sp.GetRequiredService<SmartConnectDbContext>();

        var libraryId = Guid.CreateVersion7();
        await repo.UpsertAsync(
            NewLibrary(libraryId, templates: [NewTemplate(libraryId, "t1", "x", [CodeTemplateContext.SourceTransformer])]),
            CancellationToken.None);
        Assert.Single(db.CodeTemplates);

        await repo.DeleteAsync(libraryId, CancellationToken.None);
        Assert.Empty(db.CodeTemplates);
        Assert.Empty(db.CodeTemplateLibraries);
    }

    [Fact]
    public async Task GetLinkedTemplatesForFlow_matches_by_library_LinkedFlowIds()
    {
        await using var sp = BuildServices();
        var repo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();

        var libraryId = Guid.CreateVersion7();
        var flowId = Guid.CreateVersion7();
        await SeedFlowAsync(sp, flowId, pipeline: new IntegrationFlowPipelineDefinition());
        await repo.UpsertAsync(
            NewLibrary(libraryId, linkedFlowIds: [flowId],
                templates: [NewTemplate(libraryId, "h", "function h(){}", [CodeTemplateContext.SourceTransformer])]),
            CancellationToken.None);

        var matched = await repo.GetLinkedTemplatesForFlowAsync(flowId, CodeTemplateContext.SourceTransformer, CancellationToken.None);

        Assert.Single(matched);
        Assert.Equal("h", matched[0].Name);
    }

    [Fact]
    public async Task GetLinkedTemplatesForFlow_matches_by_pipeline_LinkedLibraryIds()
    {
        await using var sp = BuildServices();
        var repo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();

        var libraryId = Guid.CreateVersion7();
        var flowId = Guid.CreateVersion7();
        var pipeline = new IntegrationFlowPipelineDefinition { LinkedLibraryIds = { libraryId } };
        await SeedFlowAsync(sp, flowId, pipeline);

        await repo.UpsertAsync(
            NewLibrary(libraryId, linkedFlowIds: [],
                templates: [NewTemplate(libraryId, "h", "function h(){}", [CodeTemplateContext.SourceTransformer])]),
            CancellationToken.None);

        var matched = await repo.GetLinkedTemplatesForFlowAsync(flowId, CodeTemplateContext.SourceTransformer, CancellationToken.None);
        Assert.Single(matched);
    }

    [Fact]
    public async Task GetLinkedTemplatesForFlow_filters_by_context()
    {
        await using var sp = BuildServices();
        var repo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();

        var libraryId = Guid.CreateVersion7();
        var flowId = Guid.CreateVersion7();
        await SeedFlowAsync(sp, flowId, new IntegrationFlowPipelineDefinition());

        await repo.UpsertAsync(NewLibrary(libraryId, linkedFlowIds: [flowId], templates:
            [
                NewTemplate(libraryId, "src", "x", [CodeTemplateContext.SourceTransformer]),
                NewTemplate(libraryId, "dst", "x", [CodeTemplateContext.DestinationTransformer]),
            ]), CancellationToken.None);

        var srcMatches = await repo.GetLinkedTemplatesForFlowAsync(flowId, CodeTemplateContext.SourceTransformer, CancellationToken.None);
        var dstMatches = await repo.GetLinkedTemplatesForFlowAsync(flowId, CodeTemplateContext.DestinationTransformer, CancellationToken.None);

        Assert.Single(srcMatches);
        Assert.Equal("src", srcMatches[0].Name);
        Assert.Single(dstMatches);
        Assert.Equal("dst", dstMatches[0].Name);
    }

    // ---- helpers ----

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddSmartConnectPersistenceInMemory(databaseName: $"sc_ctlib_{Guid.NewGuid():N}");
        return services.BuildServiceProvider();
    }

    private static async Task SeedFlowAsync(IServiceProvider sp, Guid flowId, IntegrationFlowPipelineDefinition pipeline)
    {
        var db = sp.GetRequiredService<SmartConnectDbContext>();
        db.IntegrationFlows.Add(new IntegrationFlowEntity
        {
            Id = flowId,
            Name = "test-flow",
            RuntimeState = (int)FlowRuntimeState.Started,
            PipelineJson = PipelineJsonSerializer.Serialize(pipeline),
        });
        await db.SaveChangesAsync();
    }

    private static CodeTemplateLibrary NewLibrary(
        Guid id,
        string name = "My lib",
        IReadOnlyList<Guid>? linkedFlowIds = null,
        IReadOnlyList<CodeTemplate>? templates = null) =>
        new()
        {
            Id = id,
            Name = name,
            LinkedFlowIds = linkedFlowIds ?? [],
            Templates = templates ?? [],
            LastModifiedUtc = DateTimeOffset.UtcNow,
        };

    private static CodeTemplate NewTemplate(
        Guid libraryId,
        string name,
        string code,
        IReadOnlyList<CodeTemplateContext> contexts) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            LibraryId = libraryId,
            Name = name,
            Code = code,
            Contexts = contexts,
            LastModifiedUtc = DateTimeOffset.UtcNow,
        };
}
