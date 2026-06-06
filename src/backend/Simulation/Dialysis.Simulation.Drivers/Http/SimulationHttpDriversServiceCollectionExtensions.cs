using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dialysis.Simulation.Drivers.Http;

/// <summary>Registers the live HTTP drivers that call the real module APIs.</summary>
public static class SimulationHttpDriversServiceCollectionExtensions
{
    /// <summary>
    /// Registers the HTTP drivers: a client-credentials token provider + bearer handler and one typed
    /// <see cref="HttpClient"/> per module (base addresses resolved via Aspire service discovery).
    /// </summary>
    public static IServiceCollection AddSimulationHttpDrivers(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SimulationDriverOptions>(configuration.GetSection(SimulationDriverOptions.SectionName));

        services.AddHttpClient("simulation-token");
        services.AddSingleton<IClientCredentialsTokenProvider, KeycloakClientCredentialsTokenProvider>();
        services.AddTransient<SimulationBearerTokenHandler>();

        services.AddHttpClient<IEhrDriver, HttpEhrDriver>((sp, c) => c.BaseAddress = EhrBase(sp))
            .AddHttpMessageHandler<SimulationBearerTokenHandler>();
        services.AddHttpClient<IHisDriver, HttpHisDriver>((sp, c) => c.BaseAddress = HisBase(sp))
            .AddHttpMessageHandler<SimulationBearerTokenHandler>();
        services.AddHttpClient<ILabDriver, HttpLabDriver>((sp, c) => c.BaseAddress = LabBase(sp))
            .AddHttpMessageHandler<SimulationBearerTokenHandler>();
        services.AddHttpClient<IHieDriver, HttpHieDriver>((sp, c) => c.BaseAddress = HieBase(sp))
            .AddHttpMessageHandler<SimulationBearerTokenHandler>();

        return services;
    }

    private static SimulationDriverOptions Opt(IServiceProvider sp) =>
        sp.GetRequiredService<IOptions<SimulationDriverOptions>>().Value;

    private static Uri EhrBase(IServiceProvider sp) => new(Opt(sp).EhrBaseAddress);

    private static Uri HisBase(IServiceProvider sp) => new(Opt(sp).HisBaseAddress);

    private static Uri LabBase(IServiceProvider sp) => new(Opt(sp).LabBaseAddress);

    private static Uri HieBase(IServiceProvider sp) => new(Opt(sp).HieBaseAddress);
}
