using Dialysis.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.Alerting.Data;

public interface ITenantAlertDbContextFactory
{
    AlertDbContext CreateDbContext();
}

public sealed class TenantAlertDbContextFactory : ITenantAlertDbContextFactory
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantConnectionResolver _connectionResolver;

    public TenantAlertDbContextFactory(ITenantContext tenantContext, ITenantConnectionResolver connectionResolver)
    {
        _tenantContext = tenantContext;
        _connectionResolver = connectionResolver;
    }

    public AlertDbContext CreateDbContext()
    {
        var tenantId = _tenantContext.TenantId ?? "default";
        var connectionString = _connectionResolver.GetConnectionString(tenantId);
        var options = new DbContextOptionsBuilder<AlertDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new AlertDbContext(options);
    }
}
