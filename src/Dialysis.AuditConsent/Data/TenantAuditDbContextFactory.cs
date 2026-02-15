using Dialysis.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.AuditConsent.Data;

public interface ITenantAuditDbContextFactory
{
    AuditDbContext CreateDbContext();
}

public sealed class TenantAuditDbContextFactory : ITenantAuditDbContextFactory
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantConnectionResolver _connectionResolver;

    public TenantAuditDbContextFactory(ITenantContext tenantContext, ITenantConnectionResolver connectionResolver)
    {
        _tenantContext = tenantContext;
        _connectionResolver = connectionResolver;
    }

    public AuditDbContext CreateDbContext()
    {
        var tenantId = _tenantContext.TenantId ?? "default";
        var connectionString = _connectionResolver.GetConnectionString(tenantId);
        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new AuditDbContext(options);
    }
}
