using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Transponder;

public static class TransponderSagaBuilderExtensions
{
    /// <summary>
    /// Registers a saga handler for <typeparamref name="TMessage"/> with durable state <typeparamref name="TState"/>.
    /// Registers <see cref="InMemoryTransponderSagaStore"/> only if no <see cref="ITransponderSagaStore"/> is already in DI. For production, call
    /// <c>AddTransponderEfSagaStore&lt;TContext&gt;()</c> (from the EF persistence package) <em>before</em> <c>AddTransponder</c> so this method does not register the in-memory store.
    /// </summary>
    public static TransponderBuilder AddSagaMessageHandler<TState, TMessage, THandler>(this TransponderBuilder builder)
        where TState : class, new()
        where TMessage : class
        where THandler : class, ITransponderSagaMessageHandler<TState, TMessage>
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.TryAddSingleton<ITransponderSagaStore, InMemoryTransponderSagaStore>();
        TransponderConsumeRouteRegistration.Register<TMessage>(builder.Services);
        builder.Services.TryAddScoped<THandler>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IConsumer<TMessage>, TransponderSagaConsumer<TState, TMessage, THandler>>());
        return builder;
    }
}
