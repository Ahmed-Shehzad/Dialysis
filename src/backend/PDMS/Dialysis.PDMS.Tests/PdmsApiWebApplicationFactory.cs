using Dialysis.Module.Hosting.Testing;
using Dialysis.PDMS.Persistence;
using Xunit;

namespace Dialysis.PDMS.Tests;

/// <summary>
/// Per-fixture PostgreSQL Testcontainer wired to <see cref="PdmsDbContext"/> via the shared
/// <see cref="ModuleWebApplicationFactory{TEntryPoint,TDbContext}"/> base. Used by the API
/// persistence integration tests, which need a provider that actually enforces the unique
/// indexes the in-memory provider ignores.
/// </summary>
public sealed class PdmsApiWebApplicationFactory : ModuleWebApplicationFactory<Program, PdmsDbContext>
{
    protected override string ModuleSlug => "pdms";

    protected override string ConnectionStringName => "Pdms";
}

/// <summary>xUnit collection so the Postgres container is shared across the PDMS integration tests.</summary>
[CollectionDefinition(nameof(PdmsPostgresFixtureCollection))]
public sealed class PdmsPostgresFixtureCollection : ICollectionFixture<PdmsApiWebApplicationFactory>;
