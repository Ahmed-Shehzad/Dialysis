using Microsoft.Extensions.DependencyInjection;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports;

/// <summary>
/// Provides a fluent builder for configuring transport registrations.
/// </summary>
public sealed class TransponderTransportBuilder
{
    private readonly IServiceCollection _services;

    public TransponderTransportBuilder(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Registers the core transport services.
    /// </summary>
    internal void Build()
    {
        _ = _services.AddSingleton<ITransportRegistry>(sp =>
            new TransportRegistry(sp.GetServices<ITransportFactory>()));
        _ = _services.AddSingleton<ITransportHostProvider, TransportHostProvider>();
    }

    /// <summary>
    /// Registers a transport factory in the container.
    /// </summary>
    public TransponderTransportBuilder AddTransportFactory<TFactory>()
        where TFactory : class, ITransportFactory
    {
        _ = _services.AddSingleton<ITransportFactory, TFactory>();
        return this;
    }

    /// <summary>
    /// Registers transport host settings and the host instance.
    /// </summary>
    public TransponderTransportBuilder AddTransportHost<TSettings, THost>(
        Func<IServiceProvider, TSettings> settingsFactory,
        Func<IServiceProvider, TSettings, THost> hostFactory)
        where TSettings : class, ITransportHostSettings
        where THost : class, ITransportHost
    {
        ArgumentNullException.ThrowIfNull(settingsFactory);
        ArgumentNullException.ThrowIfNull(hostFactory);

        _ = _services.AddSingleton(settingsFactory);
        _ = _services.AddSingleton(sp => hostFactory(sp, sp.GetRequiredService<TSettings>()));
        _ = _services.AddSingleton<ITransportHost>(sp => sp.GetRequiredService<THost>());
        return this;
    }
}
