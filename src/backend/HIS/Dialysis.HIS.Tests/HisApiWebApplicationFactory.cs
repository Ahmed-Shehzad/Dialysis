using Dialysis.HIS.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Dialysis.HIS.Tests;

/// <summary>
/// Spins up a per-fixture PostgreSQL container and points <see cref="HisDbContext"/> at it.
/// One container shared across all tests inside the same xUnit collection.
/// </summary>
public sealed class HisApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("dialysis_his_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync().ConfigureAwait(false);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:His", _postgres.GetConnectionString());
        builder.UseSetting("His:Authentication:Authority", string.Empty);
        builder.UseSetting("His:Authentication:RequireAuthorityWhenNotDevelopment", "false");
        builder.UseSetting("His:Transponder:EnableOutboxRelay", "false");
        builder.UseSetting("His:Transponder:RabbitMq:ConnectionUri", string.Empty);
        builder.UseEnvironment("Development");
    }
}

[CollectionDefinition(nameof(HisFixtureCollection))]
public sealed class HisFixtureCollection : ICollectionFixture<HisApiWebApplicationFactory>;
