using Dialysis.HIE.Persistence;
using Dialysis.Module.Hosting.Testing;
using Xunit;

namespace Dialysis.HIE.Tests;

/// <summary>
/// Per-fixture PostgreSQL Testcontainer wired to <see cref="HieDbContext"/> via the shared
/// <see cref="ModuleWebApplicationFactory{TEntryPoint,TDbContext}"/> base. The default HIE test
/// project runs in-memory; this factory is for the idempotency tests that must reproduce real
/// unique/primary-key conflicts (the in-memory provider does not enforce those indexes).
/// </summary>
public sealed class HiePostgresApiWebApplicationFactory : ModuleWebApplicationFactory<Program, HieDbContext>
{
    protected override string ModuleSlug => "hie";

    protected override string ConnectionStringName => "Hie";
}

/// <summary>xUnit collection so the Postgres container is shared across the HIE idempotency tests.</summary>
[CollectionDefinition(nameof(HiePostgresFixtureCollection))]
public sealed class HiePostgresFixtureCollection : ICollectionFixture<HiePostgresApiWebApplicationFactory>;
