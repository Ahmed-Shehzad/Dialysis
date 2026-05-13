using Dialysis.Identity.Persistence;
using Dialysis.Module.Hosting.Testing;
using Xunit;

namespace Dialysis.Identity.Tests;

/// <summary>
/// Per-fixture PostgreSQL Testcontainer wired to <see cref="IdentityDbContext"/> via the shared
/// <see cref="ModuleWebApplicationFactory{TEntryPoint,TDbContext}"/> base.
/// </summary>
public sealed class IdentityApiWebApplicationFactory : ModuleWebApplicationFactory<Program, IdentityDbContext>
{
    protected override string ModuleSlug => "identity";

    protected override string ConnectionStringName => "Identity";
}

[CollectionDefinition(nameof(IdentityFixtureCollection))]
public sealed class IdentityFixtureCollection : ICollectionFixture<IdentityApiWebApplicationFactory>;
