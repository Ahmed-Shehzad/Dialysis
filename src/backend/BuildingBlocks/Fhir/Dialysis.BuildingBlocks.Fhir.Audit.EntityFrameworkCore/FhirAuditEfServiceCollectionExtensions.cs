using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Fhir.Audit.EntityFrameworkCore;

public static class FhirAuditEfServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Replaces the default <see cref="IAuditEventStore"/> with an EF Core implementation bound to
        /// the module's <typeparamref name="TDbContext"/>. The module is responsible for applying
        /// <see cref="AuditEventRecordConfiguration"/> in its <c>OnModelCreating</c> override.
        /// </summary>
        public IServiceCollection AddFhirAuditEntityFrameworkStore<TDbContext>()
            where TDbContext : DbContext
        {
            services.AddScoped<IAuditEventStore, EfAuditEventStore<TDbContext>>();
            return services;
        }
    }
}
