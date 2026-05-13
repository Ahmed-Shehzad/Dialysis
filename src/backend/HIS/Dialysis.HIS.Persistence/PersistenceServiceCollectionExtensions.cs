using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.DataServices.Ports;
using Dialysis.HIS.Integration.DeviceIngestion;
using Dialysis.HIS.Operations.Ports;
using Dialysis.HIS.Persistence.Repositories;
using Dialysis.HIS.RaCapabilities.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.HIS.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers EF Core <see cref="HisDbContext"/> against PostgreSQL, repositories for the surviving facility-level
    /// bounded contexts (Operations, DataServices, Integration, RaCapabilities), and the Transponder outbox/inbox on the
    /// same context. Caller supplies the connection provider via <paramref name="configure"/>.
    /// Add migrations: <c>dotnet ef migrations add &lt;Name&gt; --project Dialysis.HIS.Persistence --startup-project Dialysis.HIS.Persistence --output-dir Migrations</c>.
    /// </summary>
    public static IServiceCollection AddHisPersistence(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder>? configure = null)
    {
        services.AddOptions<TransponderPersistenceOptions>()
            .Configure(o => o.Schema = "transponder");

        services.AddDbContext<HisDbContext>((sp, options) =>
        {
            configure?.Invoke(options);

            var interceptor = sp.GetService<AuditSaveChangesInterceptor>();
            if (interceptor is not null)
                options.AddInterceptors(interceptor);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<HisDbContext>());
        services.AddTransponderEfOutboxAndInbox<HisDbContext>();

        services.AddScoped<IStaffRepository, EfStaffRepository>();
        services.AddScoped<IInventoryRepository, EfInventoryRepository>();
        services.AddScoped<IDataImportJobRepository, EfDataImportJobRepository>();
        services.AddScoped<IDeviceReadingRepository, EfDeviceReadingRepository>();
        services.AddScoped<IIntegrationOutboxMetadataReadModel, EfIntegrationOutboxMetadataReadModel>();
        services.AddScoped<IRaCapabilitiesReadStore, EfRaCapabilitiesReadStore>();
        services.AddScoped<IRaCapabilityCommandStore, EfRaCapabilityCommandStore>();
        return services;
    }
}
