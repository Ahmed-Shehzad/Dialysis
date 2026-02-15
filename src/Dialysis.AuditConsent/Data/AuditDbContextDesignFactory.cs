using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Dialysis.AuditConsent.Data;

public sealed class AuditDbContextDesignFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    public AuditDbContext CreateDbContext(string[] args)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..");
        var config = new ConfigurationBuilder()
            .SetBasePath(path)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var template = config["Tenancy:ConnectionStringTemplate"]
            ?? "Host=localhost;Port=5432;Database=dialysis_audit_{TenantId};Username=postgres;Password=postgres";
        var conn = template.Replace("{TenantId}", "default");

        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseNpgsql(conn)
            .Options;

        return new AuditDbContext(options);
    }
}
