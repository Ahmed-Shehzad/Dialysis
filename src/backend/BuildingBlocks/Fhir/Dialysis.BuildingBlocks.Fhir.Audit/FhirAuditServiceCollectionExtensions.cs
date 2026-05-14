using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Fhir.Audit;

public static class FhirAuditServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IAuditEventEmitter"/> + <see cref="IAuditEventStore"/> (in-memory default).
    /// Override the store with a persistent implementation via
    /// <c>services.Replace(ServiceDescriptor.Singleton&lt;IAuditEventStore, MyStore&gt;())</c>.
    /// </summary>
    public static IServiceCollection AddFhirAudit(this IServiceCollection services)
    {
        services.TryAddSingleton<IAuditEventStore>(new InMemoryAuditEventStore());
        services.TryAddSingleton<IAuditEventEmitter, DefaultAuditEventEmitter>();
        return services;
    }
}
