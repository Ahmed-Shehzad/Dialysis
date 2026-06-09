using Dialysis.EHR.Api;
using Dialysis.EHR.Persistence;
using Dialysis.Module.Hosting.Testing;

namespace Dialysis.EHR.Tests;

/// <summary>
/// Per-fixture PostgreSQL Testcontainer wired to <see cref="EhrDbContext"/> via the shared
/// <see cref="ModuleWebApplicationFactory{TEntryPoint,TDbContext}"/> base.
/// </summary>
public sealed class EhrApiWebApplicationFactory : ModuleWebApplicationFactory<Program, EhrDbContext>
{
    protected override string ModuleSlug => "ehr";

    protected override string ConnectionStringName => "Ehr";
}
