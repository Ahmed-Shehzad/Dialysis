using Dialysis.HIE.Core.Abstraction.Partners;
using Dialysis.HIE.Persistence;
using Dialysis.HIE.Tests.Outbound;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Dialysis.HIE.Tests;

/// <summary>
/// Boots the HIE host with the EF in-memory provider so tests don't need Postgres,
/// disables the background dispatcher hosted service, and registers a stub partner endpoint.
/// </summary>
public sealed class HieWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"hie-tests-{Guid.NewGuid():N}";

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Hie"] = string.Empty,
            ["Hie:Authentication:Authority"] = string.Empty,
            ["Hie:Authentication:RequireAuthorityWhenNotDevelopment"] = "false",
            ["Hie:Outbound:EmitDeliveryEvents"] = "false",
        }));
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<HieDbContext>>();
            services.AddDbContext<HieDbContext>(o => o.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IHostedService>();
            services.AddSingleton<StubPartnerEndpoint>();
            services.AddSingleton<IPartnerEndpoint>(sp => sp.GetRequiredService<StubPartnerEndpoint>());
        });
    }
}
