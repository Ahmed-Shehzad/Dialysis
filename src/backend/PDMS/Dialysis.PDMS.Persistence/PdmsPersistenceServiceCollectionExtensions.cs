using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.Persistence.Stores;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.PDMS.Persistence;

public static class PdmsPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPdmsPersistence(
        this IServiceCollection services,
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

        return services;
    }
}
