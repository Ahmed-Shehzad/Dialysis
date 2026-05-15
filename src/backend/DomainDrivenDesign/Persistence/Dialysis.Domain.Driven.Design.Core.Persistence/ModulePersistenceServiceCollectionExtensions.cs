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
    extension(IServiceCollection services)
    {
        public IServiceCollection AddModuleAuditing<TActor>()
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
        public IServiceCollection AddSystemModuleAuditing()
        {
            services.TryAddSingleton(TimeProvider.System);
            services.TryAddScoped<IAuditActorAccessor, NullAuditActorAccessor>();
            services.TryAddScoped<AuditSaveChangesInterceptor>();
            return services;
        }
        /// <summary>
        /// Registers <see cref="IntegrationEventOutboxSaveChangesInterceptor"/> as scoped so the module's
        /// <c>DbContextOptionsBuilder</c> can attach it via <c>AddInterceptors</c>. Requires
        /// <c>AddTransponder(...)</c> + <c>AddTransponderEfOutboxAndInbox&lt;TContext&gt;()</c> to have run
        /// previously (those register the message serializer and the per-context outbox writer).
        /// </summary>
        public IServiceCollection AddModuleIntegrationEventOutbox()
        {
            services.TryAddScoped<IntegrationEventOutboxSaveChangesInterceptor>();
            return services;
        }
    }
}
