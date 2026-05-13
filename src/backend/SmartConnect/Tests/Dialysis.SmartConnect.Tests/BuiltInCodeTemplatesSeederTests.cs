using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class BuiltInCodeTemplatesSeederTests
{
    [Fact]
    public async Task First_run_seeds_six_templates_under_well_known_library_id()
    {
        await using var sp = BuildServices();
        var seeder = new BuiltInCodeTemplatesSeeder(
            sp.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BuiltInCodeTemplatesSeeder>.Instance);

        await seeder.StartAsync(CancellationToken.None);

        var repo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();
        var lib = await repo.GetByIdAsync(BuiltInCodeTemplatesSeeder.BuiltInLibraryId, CancellationToken.None);
        Assert.NotNull(lib);
        Assert.Equal(6, lib!.Templates.Count);
        Assert.True(lib.AutoLinkNewFlows);
        Assert.Contains(lib.Templates, t => t.Name == "formatHl7Date");
        Assert.Contains(lib.Templates, t => t.Name == "makeAck");
    }

    [Fact]
    public async Task Second_run_is_idempotent()
    {
        await using var sp = BuildServices();
        var seeder = new BuiltInCodeTemplatesSeeder(
            sp.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BuiltInCodeTemplatesSeeder>.Instance);

        await seeder.StartAsync(CancellationToken.None);
        await seeder.StartAsync(CancellationToken.None);

        var repo = sp.GetRequiredService<ICodeTemplateLibraryRepository>();
        var lib = await repo.GetByIdAsync(BuiltInCodeTemplatesSeeder.BuiltInLibraryId, CancellationToken.None);
        Assert.NotNull(lib);
        Assert.Equal(6, lib!.Templates.Count); // not doubled.
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSmartConnectPersistenceInMemory(databaseName: $"sc_seed_{Guid.NewGuid():N}");
        return services.BuildServiceProvider();
    }
}
