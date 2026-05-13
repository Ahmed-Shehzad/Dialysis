using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.Persistence;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class CodeTemplateLinkageServiceTests
{
    [Fact]
    public async Task LibraryWrite_adds_libraryId_to_each_newly_linked_flow_pipeline()
    {
        await using var sp = BuildServices();
        var flowRepo = sp.GetRequiredService<IIntegrationFlowRepository>();
        var libRepo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();
        var service = new CodeTemplateLinkageService(libRepo, flowRepo);

        var flowId = Guid.CreateVersion7();
        await flowRepo.AddAsync(NewFlow(flowId), CancellationToken.None);

        var libraryId = Guid.CreateVersion7();
        await libRepo.UpsertAsync(NewLibrary(libraryId, linkedFlowIds: [flowId]), CancellationToken.None);

        await service.ReconcileLibraryWriteAsync(libraryId, [], [flowId], CancellationToken.None);

        var flow = await flowRepo.GetByIdAsync(flowId, CancellationToken.None);
        Assert.Contains(libraryId, flow!.Pipeline.LinkedLibraryIds);
    }

    [Fact]
    public async Task LibraryWrite_removes_libraryId_from_dropped_flow()
    {
        await using var sp = BuildServices();
        var flowRepo = sp.GetRequiredService<IIntegrationFlowRepository>();
        var libRepo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();
        var service = new CodeTemplateLinkageService(libRepo, flowRepo);

        var libraryId = Guid.CreateVersion7();
        var flowId = Guid.CreateVersion7();
        var pipeline = new IntegrationFlowPipelineDefinition { LinkedLibraryIds = { libraryId } };
        await flowRepo.AddAsync(NewFlow(flowId, pipeline), CancellationToken.None);
        await libRepo.UpsertAsync(NewLibrary(libraryId, linkedFlowIds: []), CancellationToken.None);

        await service.ReconcileLibraryWriteAsync(libraryId, [flowId], [], CancellationToken.None);

        var flow = await flowRepo.GetByIdAsync(flowId, CancellationToken.None);
        Assert.DoesNotContain(libraryId, flow!.Pipeline.LinkedLibraryIds);
    }

    [Fact]
    public async Task FlowWrite_mirrors_link_into_each_library_LinkedFlowIds()
    {
        await using var sp = BuildServices();
        var flowRepo = sp.GetRequiredService<IIntegrationFlowRepository>();
        var libRepo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();
        var service = new CodeTemplateLinkageService(libRepo, flowRepo);

        var libraryId = Guid.CreateVersion7();
        var flowId = Guid.CreateVersion7();
        await libRepo.UpsertAsync(NewLibrary(libraryId, linkedFlowIds: []), CancellationToken.None);
        await flowRepo.AddAsync(NewFlow(flowId), CancellationToken.None);

        await service.ReconcileFlowWriteAsync(flowId, [], [libraryId], CancellationToken.None);

        var lib = await libRepo.GetByIdAsync(libraryId, CancellationToken.None);
        Assert.Contains(flowId, lib!.LinkedFlowIds);
    }

    [Fact]
    public async Task ApplyAutoLink_links_auto_libraries_to_new_flow()
    {
        await using var sp = BuildServices();
        var flowRepo = sp.GetRequiredService<IIntegrationFlowRepository>();
        var libRepo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();
        var service = new CodeTemplateLinkageService(libRepo, flowRepo);

        var autoLibId = Guid.CreateVersion7();
        var nonAutoLibId = Guid.CreateVersion7();
        await libRepo.UpsertAsync(NewLibrary(autoLibId, autoLinkNewFlows: true), CancellationToken.None);
        await libRepo.UpsertAsync(NewLibrary(nonAutoLibId, autoLinkNewFlows: false), CancellationToken.None);

        var flowId = Guid.CreateVersion7();
        await flowRepo.AddAsync(NewFlow(flowId), CancellationToken.None);

        await service.ApplyAutoLinkOnFlowCreateAsync(flowId, CancellationToken.None);

        var flow = await flowRepo.GetByIdAsync(flowId, CancellationToken.None);
        Assert.Contains(autoLibId, flow!.Pipeline.LinkedLibraryIds);
        Assert.DoesNotContain(nonAutoLibId, flow.Pipeline.LinkedLibraryIds);

        var autoLib = await libRepo.GetByIdAsync(autoLibId, CancellationToken.None);
        Assert.Contains(flowId, autoLib!.LinkedFlowIds);
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddSmartConnectPersistenceInMemory(databaseName: $"sc_link_{Guid.NewGuid():N}");
        return services.BuildServiceProvider();
    }

    private static IntegrationFlow NewFlow(Guid id, IntegrationFlowPipelineDefinition? pipeline = null) =>
        new()
        {
            Id = id,
            Name = "test-flow",
            RuntimeState = FlowRuntimeState.Started,
            Pipeline = pipeline ?? new IntegrationFlowPipelineDefinition(),
        };

    private static CodeTemplateLibrary NewLibrary(
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
