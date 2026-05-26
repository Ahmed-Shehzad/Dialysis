using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Fhir.Audit;

public static class FhirAuditServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="IAuditEventEmitter"/> + <see cref="IAuditEventStore"/> (in-memory default).
        /// Override the store with a persistent implementation via the EF helper
        /// <c>services.AddFhirAuditEntityFrameworkStore&lt;TDbContext&gt;()</c>; the emitter is registered
        /// <c>Scoped</c> so it can hold the Scoped EF-backed store without a captive-dependency hazard.
        /// </summary>
        public IServiceCollection AddFhirAudit()
        {
            // Singleton in-memory default keeps simple tests free of any DbContext requirement.
            // When a Scoped EF store gets registered later it is registered AFTER this one and wins
            // resolution; the Scoped emitter below resolves the Scoped store in the same scope.
            services.TryAddSingleton<IAuditEventStore>(new InMemoryAuditEventStore());
            services.TryAddScoped<IAuditEventEmitter, DefaultAuditEventEmitter>();
            return services;
        }
    }
}
