namespace Dialysis.Tenancy;

public interface ITenantConnectionResolver
{
    string GetConnectionString(string? tenantId);
}
