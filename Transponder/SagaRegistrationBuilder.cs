using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Transponder.Abstractions;
using Transponder.Persistence;
using Transponder.Persistence.Abstractions;
using Transponder.Transports.Abstractions;

namespace Transponder;

/// <summary>
/// Registers sagas and their receive endpoints.
/// </summary>
public sealed class SagaRegistrationBuilder
{
    private readonly IServiceCollection _services;
    private readonly SagaStyle _style;
    private readonly List<SagaMessageRegistration> _registrations = [];

    internal SagaRegistrationBuilder(IServiceCollection services, SagaStyle style)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _style = style;
    }

    /// <summary>
    /// Adds a saga and configures its message endpoints.
    /// </summary>
    public SagaRegistrationBuilder AddSaga<TSaga, TState>(
        Action<SagaEndpointBuilder<TSaga, TState>> configure)
        where TSaga : class
        where TState : class, ISagaState, new()
    {
        ArgumentNullException.ThrowIfNull(configure);

        _ = _services.AddScoped<TSaga>();
        _services.TryAddSingleton<ISagaRepository<TState>, InMemorySagaRepository<TState>>();

        var endpointBuilder = new SagaEndpointBuilder<TSaga, TState>(_style);
        configure(endpointBuilder);
        _registrations.AddRange(endpointBuilder.Build());

        return this;
    }

    internal SagaRegistration Build()
    {
        _services.TryAddSingleton<SagaEndpointRegistry>();
        _services.TryAddSingleton<SagaReceiveEndpointGroup>();
        _services.TryAddEnumerable(ServiceDescriptor.Singleton<IReceiveEndpoint, SagaReceiveEndpointGroup>());

        return new SagaRegistration(_registrations);
    }
}
