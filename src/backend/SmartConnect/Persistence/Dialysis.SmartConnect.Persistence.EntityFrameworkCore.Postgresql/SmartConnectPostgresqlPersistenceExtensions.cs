using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql;

/// <summary>PostgreSQL persistence plugin for SmartConnect.</summary>
public static class SmartConnectPostgresqlPersistenceExtensions
{
    /// <summary>Registers SmartConnect persistence against PostgreSQL.</summary>
    public static IServiceCollection AddSmartConnectPersistenceForPostgresql(
        this IServiceCollection services,
        string connectionString) =>
        services.AddSmartConnectPersistence(o => o.UseNpgsql(connectionString));
}
