using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Fhir.BulkData.EntityFrameworkCore;

public static class FhirBulkDataEfServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the default <see cref="IExportJobStore"/> with an EF Core implementation bound to
    /// the module's <typeparamref name="TDbContext"/>. The module is responsible for applying
    /// <see cref="ExportJobRecordConfiguration"/> in its <c>OnModelCreating</c> override.
    /// </summary>
    public static IServiceCollection AddFhirBulkDataEntityFrameworkStore<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IExportJobStore, EfExportJobStore<TDbContext>>();
        return services;
    }
}
