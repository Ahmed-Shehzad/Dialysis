using Dialysis.HIS.Persistence;
using Dialysis.Module.Hosting.Testing;

namespace Dialysis.HIS.Tests;

/// <summary>
/// Per-fixture PostgreSQL Testcontainer wired to <see cref="HisDbContext"/> via the shared
/// <see cref="ModuleWebApplicationFactory{TEntryPoint,TDbContext}"/> base.
/// </summary>
public sealed class HisApiWebApplicationFactory : ModuleWebApplicationFactory<Program, HisDbContext>
{
    protected override string ModuleSlug => "his";

    protected override string ConnectionStringName => "His";
}

[CollectionDefinition(nameof(HisFixtureCollection))]
public sealed class HisFixtureCollection : ICollectionFixture<HisApiWebApplicationFactory>;
