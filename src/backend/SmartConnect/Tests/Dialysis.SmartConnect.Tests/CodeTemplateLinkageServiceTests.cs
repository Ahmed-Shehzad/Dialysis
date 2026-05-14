using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class CodeTemplateLinkageServiceTests
{
    [Fact]
    public async Task Librarywrite_Adds_Libraryid_To_Each_Newly_Linked_Flow_Pipeline_Async()
    {
        await using var sp = Build_Services();
        var flowRepo = sp.GetRequiredService<IIntegrationFlowRepository>();
        var libRepo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();
        var service = new CodeTemplateLinkageService(libRepo, flowRepo);

        var flowId = Guid.CreateVersion7();
        await flowRepo.AddAsync(New_Flow(flowId), CancellationToken.None);

        var libraryId = Guid.CreateVersion7();
        await libRepo.UpsertAsync(New_Library(libraryId, linkedFlowIds: [flowId]), CancellationToken.None);

        await service.ReconcileLibraryWriteAsync(libraryId, [], [flowId], CancellationToken.None);

        var flow = await flowRepo.GetByIdAsync(flowId, CancellationToken.None);
        Assert.Contains(libraryId, flow!.Pipeline.LinkedLibraryIds);
    }

    [Fact]
    public async Task Librarywrite_Removes_Libraryid_From_Dropped_Flow_Async()
    {
        await using var sp = Build_Services();
        var flowRepo = sp.GetRequiredService<IIntegrationFlowRepository>();
        var libRepo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();
        var service = new CodeTemplateLinkageService(libRepo, flowRepo);

        var libraryId = Guid.CreateVersion7();
        var flowId = Guid.CreateVersion7();
        var pipeline = new IntegrationFlowPipelineDefinition { LinkedLibraryIds = { libraryId } };
        await flowRepo.AddAsync(New_Flow(flowId, pipeline), CancellationToken.None);
        await libRepo.UpsertAsync(New_Library(libraryId, linkedFlowIds: []), CancellationToken.None);

        await service.ReconcileLibraryWriteAsync(libraryId, [flowId], [], CancellationToken.None);

        var flow = await flowRepo.GetByIdAsync(flowId, CancellationToken.None);
        Assert.DoesNotContain(libraryId, flow!.Pipeline.LinkedLibraryIds);
    }

    [Fact]
    public async Task Flowwrite_Mirrors_Link_Into_Each_Library_Linkedflowids_Async()
    {
        await using var sp = Build_Services();
        var flowRepo = sp.GetRequiredService<IIntegrationFlowRepository>();
        var libRepo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();
        var service = new CodeTemplateLinkageService(libRepo, flowRepo);

        var libraryId = Guid.CreateVersion7();
        var flowId = Guid.CreateVersion7();
        await libRepo.UpsertAsync(New_Library(libraryId, linkedFlowIds: []), CancellationToken.None);
        await flowRepo.AddAsync(New_Flow(flowId), CancellationToken.None);

        await service.ReconcileFlowWriteAsync(flowId, [], [libraryId], CancellationToken.None);

        var lib = await libRepo.GetByIdAsync(libraryId, CancellationToken.None);
        Assert.Contains(flowId, lib!.LinkedFlowIds);
    }

    [Fact]
    public async Task Applyautolink_Links_Auto_Libraries_To_New_Flow_Async()
    {
        await using var sp = Build_Services();
        var flowRepo = sp.GetRequiredService<IIntegrationFlowRepository>();
        var libRepo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();
        var service = new CodeTemplateLinkageService(libRepo, flowRepo);

        var autoLibId = Guid.CreateVersion7();
        var nonAutoLibId = Guid.CreateVersion7();
        await libRepo.UpsertAsync(New_Library(autoLibId, autoLinkNewFlows: true), CancellationToken.None);
        await libRepo.UpsertAsync(New_Library(nonAutoLibId, autoLinkNewFlows: false), CancellationToken.None);

        var flowId = Guid.CreateVersion7();
        await flowRepo.AddAsync(New_Flow(flowId), CancellationToken.None);

        await service.ApplyAutoLinkOnFlowCreateAsync(flowId, CancellationToken.None);

        var flow = await flowRepo.GetByIdAsync(flowId, CancellationToken.None);
        Assert.Contains(autoLibId, flow!.Pipeline.LinkedLibraryIds);
        Assert.DoesNotContain(nonAutoLibId, flow.Pipeline.LinkedLibraryIds);

        var autoLib = await libRepo.GetByIdAsync(autoLibId, CancellationToken.None);
        Assert.Contains(flowId, autoLib!.LinkedFlowIds);
    }

    private static ServiceProvider Build_Services()
    {
        var services = new ServiceCollection();
        services.AddSmartConnectPersistenceInMemory(databaseName: $"sc_link_{Guid.NewGuid():N}");
        return services.BuildServiceProvider();
    }

    private static IntegrationFlow New_Flow(Guid id, IntegrationFlowPipelineDefinition? pipeline = null) =>
        new()
        {
            Id = id,
            Name = "test-flow",
            RuntimeState = FlowRuntimeState.Started,
            Pipeline = pipeline ?? new IntegrationFlowPipelineDefinition(),
        };

    private static CodeTemplateLibrary New_Library(
        Guid id,
        IReadOnlyList<Guid>? linkedFlowIds = null,
        bool autoLinkNewFlows = false) =>
        new()
        {
            Id = id,
            Name = "lib-" + id.ToString("N")[..6],
            LinkedFlowIds = linkedFlowIds ?? [],
            AutoLinkNewFlows = autoLinkNewFlows,
            Templates = [],
            LastModifiedUtc = DateTimeOffset.UtcNow,
        };
}
