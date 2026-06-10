using Dialysis.BuildingBlocks.DataProtection.Erasure;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.CompositionRoot;
using Dialysis.DomainDrivenDesign.DomainEvents;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.DataServices.Ports;
using Dialysis.HIS.Integration.DeviceIngestion;
using Dialysis.HIS.Integration.DeviceRegistry;
using Dialysis.HIS.Medication.Ports;
using Dialysis.HIS.Operations.Domain.Events;
using Dialysis.HIS.Operations.Domain.Events.Handlers;
using Dialysis.HIS.Operations.Domain.Services;
using Dialysis.HIS.Operations.Ports;
using Dialysis.HIS.PatientAccess.Ports;
using Dialysis.HIS.PatientFlow.Ports;
using Dialysis.HIS.Persistence.Erasure;
using Dialysis.HIS.Persistence.Repositories;
using Dialysis.HIS.RaCapabilities.Ports;
using Dialysis.HIS.Scheduling.Ports;
using Dialysis.HIS.Security.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.HIS.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers EF Core <see cref="HisDbContext"/> against PostgreSQL, repositories for the surviving facility-level
        /// bounded contexts (Operations, DataServices, Integration, RaCapabilities), and the Transponder outbox/inbox on the
        /// same context. Caller supplies the connection provider via <paramref name="configure"/>.
        /// Add migrations: <c>dotnet ef migrations add &lt;Name&gt; --project Dialysis.HIS.Persistence --startup-project Dialysis.HIS.Persistence --output-dir Migrations</c>.
        /// </summary>
        public IServiceCollection AddHisPersistence(
            Action<DbContextOptionsBuilder>? configure = null)
        {
            services.AddOptions<TransponderPersistenceOptions>()
                .Configure(o => o.Schema = "transponder");

            services.AddDomainEventDispatch();
            services.AddScoped<IDomainEventHandler<BillingExportJobQueuedDomainEvent>, BillingExportJobQueuedDomainEventHandler>();

            services.AddDbContext<HisDbContext>((sp, options) =>
            {
                configure?.Invoke(options);

                var audit = sp.GetService<AuditSaveChangesInterceptor>();
                if (audit is not null)
                    options.AddInterceptors(audit);

                var domainEvents = sp.GetService<DomainEventSaveChangesInterceptor>();
                if (domainEvents is not null)
                    options.AddInterceptors(domainEvents);
            });

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<HisDbContext>());
            services.AddTransponderEfOutboxAndInbox<HisDbContext>();
            services.AddScoped<IPatientEraser,
                HisPatientEraser>();

            services.AddScoped<IStaffRepository, EfStaffRepository>();
            services.AddScoped<IInventoryRepository, EfInventoryRepository>();
            services.AddScoped<IBillingExportJobRepository, EfBillingExportJobRepository>();
            services.AddScoped<IBillingExportJobAuditRepository, EfBillingExportJobAuditRepository>();
            services.AddScoped<BillingExportEligibilityService>();
            services.AddScoped<IDataImportJobRepository, EfDataImportJobRepository>();
            services.AddScoped<IDeviceReadingRepository, EfDeviceReadingRepository>();
            services.AddScoped<IDeviceRepository, EfDeviceRepository>();
            services.AddScoped<IIntegrationOutboxMetadataReadModel, EfIntegrationOutboxMetadataReadModel>();
            services.AddScoped<IPatientSearchReadModel, EfPatientSearchReadModel>();
            services.AddScoped<IManagerDashboardReadModel, EfManagerDashboardReadModel>();
            services.AddScoped<IRaCapabilitiesReadStore, EfRaCapabilitiesReadStore>();
            services.AddScoped<IRaCapabilityCommandStore, EfRaCapabilityCommandStore>();

            services.AddScoped<ILocalUserRepository, EfLocalUserRepository>();
            services.AddScoped<IAppointmentRepository, EfAppointmentRepository>();
            services.AddScoped<IAdmissionRepository, EfAdmissionRepository>();
            services.AddScoped<IMedicationOrderRepository, EfMedicationOrderRepository>();
            services.AddScoped<IPatientPortalReadModel, EfPatientPortalReadModel>();
            // Patient queue is in-memory while the workflow shape is being validated with
            // clinical staff; singleton so every request hits the same demo queue.
            services.AddSingleton<IPatientQueueRepository, InMemoryPatientQueueRepository>();
            return services;
        }
    }
}
