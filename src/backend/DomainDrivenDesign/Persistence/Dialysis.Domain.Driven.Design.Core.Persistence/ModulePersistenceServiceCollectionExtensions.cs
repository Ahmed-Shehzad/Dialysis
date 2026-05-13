using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.DomainDrivenDesign.Persistence;

/// <summary>
/// DI helpers wiring the audit interceptor and supporting services.
/// Each module calls <see cref="AddModuleAuditing{TActor}"/> in its composition root and then
/// adds <see cref="AuditSaveChangesInterceptor"/> to its <c>DbContextOptions</c>.
/// </summary>
public static class ModulePersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddModuleAuditing<TActor>(this IServiceCollection services)
        where TActor : class, IAuditActorAccessor
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<IAuditActorAccessor, TActor>();
        services.TryAddScoped<AuditSaveChangesInterceptor>();
        return services;
    }

    /// <summary>
    /// Registers a no-op audit actor accessor (useful for background workers, migrations, tests).
    /// </summary>
    public static IServiceCollection AddSystemModuleAuditing(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<IAuditActorAccessor, NullAuditActorAccessor>();
        services.TryAddScoped<AuditSaveChangesInterceptor>();
        return services;
    }
}
