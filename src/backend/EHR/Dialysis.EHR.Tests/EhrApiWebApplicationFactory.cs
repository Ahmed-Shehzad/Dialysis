using Dialysis.EHR.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Dialysis.EHR.Tests;

/// <summary>
/// Spins up a per-fixture PostgreSQL container and points <see cref="EhrDbContext"/> at it.
/// One container shared across all tests inside the same xUnit collection.
/// </summary>
public sealed class EhrApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("dialysis_ehr_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync().ConfigureAwait(false);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EhrDbContext>();
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Ehr", _postgres.GetConnectionString());
        builder.UseSetting("Ehr:Authentication:Authority", string.Empty);
        builder.UseSetting("Ehr:Transponder:EnableOutboxRelay", "false");
        builder.UseSetting("Ehr:Transponder:RabbitMq:ConnectionUri", string.Empty);
        builder.UseEnvironment("Development");
    }
}
