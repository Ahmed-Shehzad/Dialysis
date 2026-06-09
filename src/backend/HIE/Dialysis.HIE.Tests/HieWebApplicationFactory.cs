using Dialysis.HIE.Api;
using Dialysis.HIE.Core.Abstraction.Partners;
using Dialysis.HIE.Persistence;
using Dialysis.HIE.Tests.Outbound;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Dialysis.HIE.Tests;

/// <summary>
/// Boots the HIE host against a real PostgreSQL Testcontainer (shared across the assembly; each factory
/// instance gets its own fresh database for isolation), disables the background dispatcher hosted
/// services, and registers a stub partner endpoint. Replaces the former EF in-memory provider so HIE
/// integration tests exercise the same PostgreSQL store the module runs on.
/// </summary>
public sealed class HieWebApplicationFactory : WebApplicationFactory<Program>
{
    // One Postgres container for the whole HIE test assembly; reaped by Testcontainers at process exit.
    // Started synchronously (the test classes create the factory with `new`, not as an IAsyncLifetime
    // fixture, so there is no async hook); the lock makes the one-time start thread-safe across xUnit's
    // parallel test classes. CREATE DATABASE also can't run concurrently, so the same lock serializes
    // per-instance schema creation.
    private static readonly Lock _syncRoot = new();
    private static PostgreSqlContainer? _shared;

    private readonly string _databaseName = $"hie_tests_{Guid.NewGuid():N}";

    private static PostgreSqlContainer Container()
    {
        if (_shared is not null)
            return _shared;
        lock (_syncRoot)
        {
            _shared ??= StartContainer();
            return _shared;
        }
    }

    private static PostgreSqlContainer StartContainer()
    {
        var container = new PostgreSqlBuilder("postgres:17-alpine").Build();
        // Intentional synchronous block: the test classes construct this factory with `new` per test
        // (not as an IAsyncLifetime fixture), so there is no async hook to start the shared container from.
#pragma warning disable VSTHRD002
        container.StartAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        return container;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(Container().GetConnectionString())
        {
            Database = _databaseName,
        }.ConnectionString;

        builder.ConfigureHostConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Hie"] = connectionString,
            ["Hie:Authentication:Authority"] = string.Empty,
            ["Hie:Authentication:RequireAuthorityWhenNotDevelopment"] = "false",
            ["Hie:Outbound:EmitDeliveryEvents"] = "false",
        }));

        var host = base.CreateHost(builder);

        // The host's startup MigrateAsync doesn't run under WebApplicationFactory (Main is interrupted at
        // host build), so create the schema here. EnsureCreated also issues CREATE DATABASE for the fresh
        // per-instance database.
        lock (_syncRoot)
        {
            using var scope = host.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<HieDbContext>().Database.EnsureCreated();
        }

        return host;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.AddSingleton<StubPartnerEndpoint>();
            services.AddSingleton<IPartnerEndpoint>(sp => sp.GetRequiredService<StubPartnerEndpoint>());
        });
    }
}
