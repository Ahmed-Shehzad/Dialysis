using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIE.Consent;
using Dialysis.HIE.Consent.Ports;
using Dialysis.HIE.Core.Abstraction.Consent;
using Dialysis.HIE.Inbound.Ports;
using Dialysis.HIE.OpenEhr.Ports;
using Dialysis.HIE.Outbound.Ports;
using Dialysis.HIE.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.HIE.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="HieDbContext"/>, slice repositories (outbound bundles, received resources,
    /// patient index, consents, openEHR compositions), and the Transponder outbox/inbox on the same context.
    /// Add migrations: <c>dotnet ef migrations add &lt;Name&gt; --project Dialysis.HIE.Persistence --startup-project Dialysis.HIE.Api --output-dir Migrations</c>.
    /// </summary>
    public static IServiceCollection AddHiePersistence(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder>? configure = null)
    {
        services.AddOptions<TransponderPersistenceOptions>()
            .Configure(o => o.Schema = "transponder");

        services.AddDbContext<HieDbContext>((sp, options) =>
        {
            configure?.Invoke(options);

            var audit = sp.GetService<AuditSaveChangesInterceptor>();
            if (audit is not null)
                options.AddInterceptors(audit);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<HieDbContext>());
        services.AddTransponderEfOutboxAndInbox<HieDbContext>();

        services.AddScoped<IOutboundBundleStore, EfOutboundBundleStore>();
        services.AddScoped<IReceivedResourceStore, EfReceivedResourceStore>();
        services.AddScoped<IPatientIndex, EfPatientIndex>();
        services.AddScoped<IConsentRepository, EfConsentRepository>();
        services.AddScoped<ICompositionStore, EfCompositionStore>();
        services.AddScoped<IConsentGate, ConsentGate>();

        return services;
    }
}
