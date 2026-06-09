using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class CodeTemplateLibraryRepositoryTests
{
    [Fact]
    public async Task Upsert_And_Get_Round_Trip_Preserves_Templates_And_Link_Metadata_Async()
    {
        await using var sp = Build_Services();
        var repo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();

        var libraryId = Guid.CreateVersion7();
        var flowId = Guid.CreateVersion7();
        var library = New_Library(libraryId, linkedFlowIds: [flowId], templates:
            [
                New_Template(libraryId, "helpA", "function helpA(){ return 'A'; }", [CodeTemplateContext.SourceTransformer]),
                New_Template(libraryId, "helpB", "function helpB(){ return 'B'; }", [CodeTemplateContext.DestinationTransformer]),
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
    public async Task Delete_Cascades_Templates_Async()
    {
        await using var sp = Build_Services();
        var repo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();
        var db = sp.GetRequiredService<SmartConnectDbContext>();

        var libraryId = Guid.CreateVersion7();
        await repo.UpsertAsync(
            New_Library(libraryId, templates: [New_Template(libraryId, "t1", "x", [CodeTemplateContext.SourceTransformer])]),
            CancellationToken.None);
        Assert.Single(db.CodeTemplates);

        await repo.DeleteAsync(libraryId, CancellationToken.None);
        Assert.Empty(db.CodeTemplates);
        Assert.Empty(db.CodeTemplateLibraries);
    }

    [Fact]
    public async Task Getlinkedtemplatesforflow_Matches_By_Library_Linkedflowids_Async()
    {
        await using var sp = Build_Services();
        var repo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();

        var libraryId = Guid.CreateVersion7();
        var flowId = Guid.CreateVersion7();
        await Seedflow_Async(sp, flowId, pipeline: new IntegrationFlowPipelineDefinition());
        await repo.UpsertAsync(
            New_Library(libraryId, linkedFlowIds: [flowId],
                templates: [New_Template(libraryId, "h", "function h(){}", [CodeTemplateContext.SourceTransformer])]),
            CancellationToken.None);

        var matched = await repo.GetLinkedTemplatesForFlowAsync(flowId, CodeTemplateContext.SourceTransformer, CancellationToken.None);

        Assert.Single(matched);
        Assert.Equal("h", matched[0].Name);
    }

    [Fact]
    public async Task Getlinkedtemplatesforflow_Matches_By_Pipeline_Linkedlibraryids_Async()
    {
        await using var sp = Build_Services();
        var repo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();

        var libraryId = Guid.CreateVersion7();
        var flowId = Guid.CreateVersion7();
        var pipeline = new IntegrationFlowPipelineDefinition { LinkedLibraryIds = { libraryId } };
        await Seedflow_Async(sp, flowId, pipeline);

        await repo.UpsertAsync(
            New_Library(libraryId, linkedFlowIds: [],
                templates: [New_Template(libraryId, "h", "function h(){}", [CodeTemplateContext.SourceTransformer])]),
            CancellationToken.None);

        var matched = await repo.GetLinkedTemplatesForFlowAsync(flowId, CodeTemplateContext.SourceTransformer, CancellationToken.None);
        Assert.Single(matched);
    }

    [Fact]
    public async Task Getlinkedtemplatesforflow_Filters_By_Context_Async()
    {
        await using var sp = Build_Services();
        var repo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();

        var libraryId = Guid.CreateVersion7();
        var flowId = Guid.CreateVersion7();
        await Seedflow_Async(sp, flowId, new IntegrationFlowPipelineDefinition());

        await repo.UpsertAsync(New_Library(libraryId, linkedFlowIds: [flowId], templates:
            [
                New_Template(libraryId, "src", "x", [CodeTemplateContext.SourceTransformer]),
                New_Template(libraryId, "dst", "x", [CodeTemplateContext.DestinationTransformer]),
            ]), CancellationToken.None);

        var srcMatches = await repo.GetLinkedTemplatesForFlowAsync(flowId, CodeTemplateContext.SourceTransformer, CancellationToken.None);
        var dstMatches = await repo.GetLinkedTemplatesForFlowAsync(flowId, CodeTemplateContext.DestinationTransformer, CancellationToken.None);

        Assert.Single(srcMatches);
        Assert.Equal("src", srcMatches[0].Name);
        Assert.Single(dstMatches);
        Assert.Equal("dst", dstMatches[0].Name);
    }

    // ---- helpers ----

    private static ServiceProvider Build_Services()
    {
        var services = new ServiceCollection();
        services.AddSmartConnectPersistenceForPostgresql(SmartConnectPostgres.NewDatabaseConnectionString());
        return services.BuildServiceProvider();
    }

    private static async Task Seedflow_Async(IServiceProvider sp, Guid flowId, IntegrationFlowPipelineDefinition pipeline)
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

    private static CodeTemplateLibrary New_Library(
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

    private static CodeTemplate New_Template(
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
