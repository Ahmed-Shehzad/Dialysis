using Dialysis.Simulation.Drivers.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.Simulation.Drivers;

/// <summary>Registers the module drivers the simulation engine talks to.</summary>
public static class SimulationDriversServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory drivers (deterministic, no infrastructure) so a scenario runs
    /// end-to-end in dev/tests. The live HTTP drivers that call the real module APIs are wired in a
    /// later increment behind a configuration switch.
    /// </summary>
    public static IServiceCollection AddSimulationInMemoryDrivers(this IServiceCollection services)
    {
        services.AddSingleton<IEhrDriver, InMemoryEhrDriver>();
        services.AddSingleton<IHisDriver, InMemoryHisDriver>();
        services.AddSingleton<ILabDriver, InMemoryLabDriver>();
        services.AddSingleton<IHieDriver, InMemoryHieDriver>();
        return services;
    }
}
