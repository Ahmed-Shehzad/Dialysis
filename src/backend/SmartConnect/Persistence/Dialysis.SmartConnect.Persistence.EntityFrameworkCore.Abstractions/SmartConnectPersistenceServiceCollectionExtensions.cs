using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.SmartConnect.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

/// <summary>Provider-neutral SmartConnect EF Core registration. Use database-specific plugin assemblies for <c>Use*</c>.</summary>
public static class SmartConnectPersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="SmartConnectDbContext"/>, repositories, and <see cref="IUnitOfWork"/> for SmartConnect.
    /// </summary>
    public static IServiceCollection AddSmartConnectPersistence(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configure)
    {
        services.AddDbContext<SmartConnectDbContext>(configure);
        services.TryAddScoped<IIntegrationFlowRepository, EfIntegrationFlowRepository>();
        services.TryAddScoped<IMessageLedger, EfMessageLedger>();
        services.TryAddScoped<IMessageLedgerQuery, EfMessageLedgerQuery>();
        services.TryAddScoped<IMessageLedgerStatistics, EfMessageLedgerStatistics>();
        services.TryAddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SmartConnectDbContext>());
        services.TryAddScoped<IVariableMapStore, EfVariableMapStore>();
        services.TryAddScoped<IAuditEventStore, EfAuditEventStore>();
        return services;
    }
}
