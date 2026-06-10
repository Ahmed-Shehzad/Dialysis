using Dialysis.BuildingBlocks.DataProtection.Erasure;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.Persistence.Erasure;
using Dialysis.PDMS.Persistence.Stores;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.PDMS.Persistence;

public static class PdmsPersistenceServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPdmsPersistence(
        Action<DbContextOptionsBuilder>? configure = null)
        {
            services.AddOptions<TransponderPersistenceOptions>()
                .Configure(o => o.Schema = "pdms");

            services.AddDbContext<PdmsDbContext>((sp, options) =>
            {
                configure?.Invoke(options);
                var interceptor = sp.GetService<AuditSaveChangesInterceptor>();
                if (interceptor is not null)
                    options.AddInterceptors(interceptor);

                // Drain aggregate-raised integration events into the Transponder outbox in the SAME
                // transaction as the state change (transactional-outbox guarantee). Without this
                // interceptor, events raised via RaiseIntegrationEvent — DialysisSessionCompleted,
                // SessionReportGenerated, MedicationAdministered, IvPumpAlarmRaised, etc. — are only
                // appended to the aggregate's in-memory list and are never persisted, relayed, or
                // consumed. AddTransponderEfOutboxAndInbox only creates the outbox table + relay; it
                // does NOT write aggregate events into it.
                var integrationEventOutbox = sp.GetService<IntegrationEventOutboxSaveChangesInterceptor>();
                if (integrationEventOutbox is not null)
                    options.AddInterceptors(integrationEventOutbox);
            });

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PdmsDbContext>());
            services.AddTransponderEfOutboxAndInbox<PdmsDbContext>();
            services.AddModuleIntegrationEventOutbox();
            services.AddScoped<IDialysisSessionRepository, DialysisSessionRepository>();
            services.AddScoped<ITreatmentAlarmRepository, TreatmentAlarmRepository>();
            services.AddScoped<IPatientEraser,
                PdmsPatientEraser>();

            return services;
        }
    }
}
