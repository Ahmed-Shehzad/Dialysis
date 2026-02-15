using Microsoft.Extensions.DependencyInjection;

using Transponder.Transports;
using Transponder.Transports.Abstractions;

namespace Transponder;

public sealed class TransponderTransportRegistrationOptions
{
    private readonly List<Action<TransponderTransportBuilder>> _builderActions = [];
    private readonly List<Action<IServiceCollection>> _serviceActions = [];

    internal bool HasRegistrations => _builderActions.Count > 0 || _serviceActions.Count > 0;

    public void AddTransportFactory<TFactory>()
        where TFactory : class, ITransportFactory
        => _builderActions.Add(builder => builder.AddTransportFactory<TFactory>());

    public void AddTransportHost<TSettings, THost>(
        Func<IServiceProvider, TSettings> settingsFactory,
        Func<IServiceProvider, TSettings, THost> hostFactory)
        where TSettings : class, ITransportHostSettings
        where THost : class, ITransportHost
        => _builderActions.Add(builder => builder.AddTransportHost(settingsFactory, hostFactory));

    public void AddTransportHost<TTransportHost>(Func<IServiceProvider, TTransportHost> hostFactory)
        where TTransportHost : class, ITransportHost
        => _serviceActions.Add(services =>
        {
            _ = services.AddSingleton(hostFactory);
            _ = services.AddSingleton<ITransportHost>(sp => sp.GetRequiredService<TTransportHost>());
        });

    public void AddTransportHost(Func<IServiceProvider, ITransportHost> hostFactory)
        => _serviceActions.Add(services => services.AddSingleton(hostFactory));

    public void UseSagaOrchestration(Action<SagaRegistrationBuilder> configure)
        => _serviceActions.Add(services => services.UseSagaOrchestration(configure));

    public void UseSagaChoreography(Action<SagaRegistrationBuilder> configure)
        => _serviceActions.Add(services => services.UseSagaChoreography(configure));

    internal void Apply(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var hasProvider = services.Any(service => service.ServiceType == typeof(ITransportHostProvider));
        var hasRegistry = services.Any(service => service.ServiceType == typeof(ITransportRegistry));

        if (hasProvider && hasRegistry)
        {
            var builder = new TransponderTransportBuilder(services);
            foreach (var action in _builderActions) action(builder);
            foreach (var action in _serviceActions) action(services);
            return;
        }

        _ = services.AddTransponderTransports(builder =>
        {
            foreach (var action in _builderActions) action(builder);
            foreach (var action in _serviceActions) action(services);
        });
    }
}
