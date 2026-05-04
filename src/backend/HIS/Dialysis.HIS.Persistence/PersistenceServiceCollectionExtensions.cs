using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.DataServices.Ports;
using Dialysis.HIS.Integration.DeviceIngestion;
using Dialysis.HIS.Operations.Ports;
using Dialysis.HIS.PatientAccess;
using Dialysis.HIS.PatientAccess.Ports;
using Dialysis.HIS.PatientFlow.Ports;
using Dialysis.HIS.Persistence.Stores;
using Dialysis.HIS.Persistence.PatientAccess;
using Dialysis.HIS.Persistence.ReadModels;
using Dialysis.HIS.Persistence.Repositories;
using Dialysis.HIS.RaCapabilities.Ports;
using Dialysis.HIS.Contracts.PatientLifecycle;
using Dialysis.HIS.Medication;
using Dialysis.HIS.Medication.Ports;
using Dialysis.HIS.Scheduling.Ports;
using Dialysis.HIS.Security.Audit;
using Dialysis.HIS.Security.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.HIS.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers EF Core <see cref="HisDbContext"/> (default: in-memory), repositories, Transponder transactional outbox/inbox on the same context, audit trail, and read models.
    /// When <paramref name="configure"/> is null, the default in-memory store uses name <c>DialysisHIS</c>, unless configuration sets <c>His:InMemoryDatabaseName</c> (used by integration tests for isolation).
    /// For SQL Server, apply schema with <see cref="HisDatabaseInitializer"/> via <see cref="DbContext.Database.MigrateAsync"/>.
    /// Add migrations: <c>dotnet ef migrations add &lt;Name&gt; --project Dialysis.HIS.Persistence/Dialysis.HIS.Persistence.csproj --startup-project Dialysis.HIS.Persistence/Dialysis.HIS.Persistence.csproj --output-dir Migrations</c> (see <see cref="HisDbContextDesignTimeFactory"/> for how the SQL connection string is resolved).
    /// </summary>
    public static IServiceCollection AddHisPersistence(this IServiceCollection services, Action<DbContextOptionsBuilder>? configure = null)
    {
        services.AddOptions<TransponderPersistenceOptions>()
            .Configure(o => o.Schema = "transponder");

        services.AddDbContext<HisDbContext>((sp, options) =>
        {
            if (configure is not null)
            {
                configure(options);
                return;
            }

            // Optional: integration tests set <c>His:InMemoryDatabaseName</c> so parallel hosts do not share one in-memory store.
            var named = sp.GetService<IConfiguration>()?["His:InMemoryDatabaseName"];
            options.UseInMemoryDatabase(string.IsNullOrWhiteSpace(named) ? "DialysisHIS" : named);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<HisDbContext>());
        services.AddTransponderEfOutboxAndInbox<HisDbContext>();
        services.AddScoped<IAuditTrail, EfAuditTrail>();
        services.AddScoped<IUserDirectoryRepository, EfUserDirectoryRepository>();
        services.AddScoped<IPatientRepository, EfPatientRepository>();
        services.AddScoped<IReferralRepository, EfReferralRepository>();
        services.AddScoped<IAppointmentRepository, EfAppointmentRepository>();
        services.AddScoped<ISchedulingResourceDirectory, EfSchedulingResourceDirectory>();
        services.AddScoped<IMedicationOrderRepository, EfMedicationOrderRepository>();
        services.AddScoped<IMedicationOrderSafetyPolicy, FormularyMedicationOrderSafetyPolicy>();
        services.AddScoped<IStaffRepository, EfStaffRepository>();
        services.AddScoped<IInventoryRepository, EfInventoryRepository>();
        services.AddScoped<IBillingExportRepository, EfBillingExportRepository>();
        services.AddScoped<IDataImportJobRepository, EfDataImportJobRepository>();
        services.AddScoped<IPatientAppointmentRequestRepository, EfPatientAppointmentRequestRepository>();
        services.AddScoped<IDeviceReadingRepository, EfDeviceReadingRepository>();
        services.AddScoped<IPatientSearchReadModel, EfPatientSearchReadModel>();
        services.AddScoped<IManagerDashboardReadModel, EfManagerDashboardReadModel>();
        services.AddScoped<IIntegrationOutboxMetadataReadModel, EfIntegrationOutboxMetadataReadModel>();
        services.AddScoped<IPatientPortalSummaryReadModel, EfPatientPortalSummaryReadModel>();
        services.AddScoped<IPatientPortalConsentReadModel, EfPatientPortalConsentReadModel>();
        services.AddScoped<IPatientConsentGate, RuleBasedPatientConsentGate>();
        services.AddScoped<IPatientRegisteredLifecycleHook, PatientRegisteredPortalConsentBootstrap>();
        services.AddScoped<IRaCapabilitiesReadStore, EfRaCapabilitiesReadStore>();
        services.AddScoped<IRaCapabilityCommandStore, EfRaCapabilityCommandStore>();
        services.AddHostedService<HisDatabaseInitializer>();
        return services;
    }
}
