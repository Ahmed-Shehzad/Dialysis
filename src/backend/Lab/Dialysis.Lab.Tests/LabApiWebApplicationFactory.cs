using Dialysis.Lab.Api;
using Dialysis.Lab.Persistence;
using Dialysis.Module.Hosting.Testing;
using Xunit;

namespace Dialysis.Lab.Tests;

/// <summary>
/// Per-fixture PostgreSQL Testcontainer wired to <see cref="LabDbContext"/> via the shared
/// <see cref="ModuleWebApplicationFactory{TEntryPoint,TDbContext}"/> base.
/// </summary>
public sealed class LabApiWebApplicationFactory : ModuleWebApplicationFactory<Program, LabDbContext>
{
    protected override string ModuleSlug => "lab";

    protected override string ConnectionStringName => "Lab";
}

[CollectionDefinition(nameof(LabFixtureCollection))]
public sealed class LabFixtureCollection : ICollectionFixture<LabApiWebApplicationFactory>;
