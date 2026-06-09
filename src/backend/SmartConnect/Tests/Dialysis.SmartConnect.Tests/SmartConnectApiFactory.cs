using Dialysis.SmartConnect.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Boots the SmartConnect API host for integration tests against a real PostgreSQL Testcontainer (shared
/// across the assembly via <see cref="SmartConnectPostgres"/>; each factory instance gets its own freshly
/// created database). The RabbitMQ URI is blanked so the host uses the in-process Transponder bus — no
/// broker needed.
/// </summary>
public sealed class SmartConnectApiFactory : WebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:SmartConnect", SmartConnectPostgres.NewDatabaseConnectionString());
        builder.UseSetting("SmartConnect:Transponder:RabbitMq:ConnectionUri", string.Empty);
        // SmartConnectPostgres already built the schema (EnsureCreated); skip the host's startup
        // MigrateAsync so it doesn't try to re-create the tables on top of it.
        builder.UseSetting("SmartConnect:AutoMigrate", "false");
    }
}
