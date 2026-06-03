using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Persistence;
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
            });

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PdmsDbContext>());
            services.AddTransponderEfOutboxAndInbox<PdmsDbContext>();
            services.AddScoped<IDialysisSessionRepository, DialysisSessionRepository>();
            services.AddScoped<ITreatmentAlarmRepository, TreatmentAlarmRepository>();
            services.AddScoped<Dialysis.BuildingBlocks.DataProtection.Erasure.IPatientEraser,
                Dialysis.PDMS.Persistence.Erasure.PdmsPatientEraser>();

            return services;
        }
    }
}
