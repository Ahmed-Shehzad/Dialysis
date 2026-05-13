using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Dialysis.Module.Hosting.Testing;

/// <summary>
/// Base <see cref="WebApplicationFactory{TEntryPoint}"/> for module integration tests. Spins up a
/// per-fixture PostgreSQL Testcontainer, points the module's <typeparamref name="TDbContext"/> at
/// it via configuration overrides, and disables the outbox relay + RabbitMQ transport so tests
/// don't need external infrastructure. Derived classes only declare the module slug + entry-point
/// type — the rest of the wiring is shared.
/// </summary>
/// <typeparam name="TEntryPoint">The module's <c>Program</c> type (must be a public partial class for WebApplicationFactory).</typeparam>
/// <typeparam name="TDbContext">The module's <see cref="DbContext"/>; the factory calls <c>EnsureCreatedAsync</c> on it after the container starts.</typeparam>
public abstract class ModuleWebApplicationFactory<TEntryPoint, TDbContext>
    : WebApplicationFactory<TEntryPoint>, IAsyncLifetime
    where TEntryPoint : class
    where TDbContext : DbContext
{
    private readonly PostgreSqlContainer _postgres;

    protected ModuleWebApplicationFactory()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase($"dialysis_{ModuleSlug}_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    /// <summary>Module slug (e.g. <c>"ehr"</c>, <c>"his"</c>, <c>"identity"</c>). Used as the configuration section root.</summary>
    protected abstract string ModuleSlug { get; }

    /// <summary>Name of the <c>ConnectionStrings:{name}</c> entry the module reads (typically the module slug capitalized, e.g. <c>"Ehr"</c>, <c>"His"</c>).</summary>
    protected abstract string ConnectionStringName { get; }

    /// <summary>Hook for derived factories to apply additional <see cref="IWebHostBuilder"/> settings before the host is built.</summary>
    protected virtual void ConfigureModuleWebHost(IWebHostBuilder builder)
    {
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync().ConfigureAwait(false);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    protected sealed override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting($"ConnectionStrings:{ConnectionStringName}", _postgres.GetConnectionString());
        builder.UseSetting($"{ConnectionStringName}:Authentication:Authority", string.Empty);
        builder.UseSetting($"{ConnectionStringName}:Authentication:RequireAuthorityWhenNotDevelopment", "false");
        builder.UseSetting($"{ConnectionStringName}:Transponder:EnableOutboxRelay", "false");
        builder.UseSetting($"{ConnectionStringName}:Transponder:RabbitMq:ConnectionUri", string.Empty);
        builder.UseEnvironment("Development");

        ConfigureModuleWebHost(builder);
    }
}
