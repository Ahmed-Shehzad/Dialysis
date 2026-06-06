using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Simulation.Engine.Ports;
using Dialysis.Simulation.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.Simulation.Persistence;

/// <summary>Registers <see cref="SimulationDbContext"/>, the unit of work, the outbox/inbox, and the stores.</summary>
public static class SimulationPersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers persistence for the Simulation module (caller supplies the provider via
    /// <paramref name="configure"/>). Add migrations:
    /// <c>dotnet ef migrations add &lt;Name&gt; --project Dialysis.Simulation.Persistence --startup-project Dialysis.Simulation.Api --output-dir Migrations</c>.
    /// </summary>
    public static IServiceCollection AddSimulationPersistence(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder>? configure = null)
    {
        services.AddOptions<TransponderPersistenceOptions>()
            .Configure(o => o.Schema = "transponder");

        services.AddDbContext<SimulationDbContext>((_, options) => configure?.Invoke(options));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SimulationDbContext>());
        services.AddTransponderEfOutboxAndInbox<SimulationDbContext>();

        services.AddScoped<ISimulationSessionRepository, EfSimulationSessionRepository>();
        services.AddScoped<ISimulationWriteStore, EfSimulationWriteStore>();
        services.AddScoped<ISimulationQueryStore, EfSimulationQueryStore>();

        return services;
    }
}
