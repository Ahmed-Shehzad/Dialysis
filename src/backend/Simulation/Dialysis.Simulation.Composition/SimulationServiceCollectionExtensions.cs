using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.Simulation.Drivers;
using Dialysis.Simulation.Drivers.Http;
using Dialysis.Simulation.Engine.Engine;
using Dialysis.Simulation.Engine.Generation;
using Dialysis.Simulation.Engine.Scenarios;
using Dialysis.Simulation.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.Simulation.Composition;

/// <summary>Composition root for the Simulation bounded context.</summary>
public static class SimulationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Simulation bounded context: EF persistence (caller supplies the provider), the
    /// Transponder bus + EF outbox/inbox, the deterministic generator, the scenario registry, the
    /// module drivers (in-memory by default), the engine, the CQRS handlers/behaviors, and the optional
    /// outbox relay.
    /// </summary>
    public static IServiceCollection AddSimulation(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configurePersistence = null,
        bool enableOutboxRelay = false,
        Action<IServiceCollection>? configureTransponderTransport = null)
    {
        services.AddSimulationPersistence(configurePersistence);

        services.AddTransponder(_ => { });
        configureTransponderTransport?.Invoke(services);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IJourneyGenerator, BogusJourneyGenerator>();

        // Default to the deterministic in-memory drivers; opt into the live HTTP drivers (which call the
        // real module APIs and need the Aspire stack) via Simulation:Drivers:Mode=Http.
        var driverMode = configuration[$"{SimulationDriverOptions.SectionName}:Mode"];
        if (string.Equals(driverMode, nameof(SimulationDriverMode.Http), StringComparison.OrdinalIgnoreCase))
            services.AddSimulationHttpDrivers(configuration);
        else
            services.AddSimulationInMemoryDrivers();

        services.AddSingleton<IScenario, OutpatientLabScenario>();
        services.AddSingleton<IScenario, InpatientSurgeryScenario>();
        services.AddSingleton<IScenario, ReferralExchangeScenario>();
        services.AddSingleton<IScenarioRegistry, ScenarioRegistry>();

        services.AddScoped<ISimulationEngine, SimulationEngine>();

        services.AddSimulationCqrs();

        if (enableOutboxRelay)
            services.AddTransponderOutboxRelay<SimulationDbContext>();

        return services;
    }
}
