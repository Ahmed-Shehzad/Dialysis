using Dialysis.BuildingBlocks.Transponder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// Host wiring for the durable command bus. Modules call
/// <c>services.AddDurableCommandBus&lt;PdmsDbContext&gt;(b =&gt; { b.RegisterCommand&lt;Foo, FooResult&gt;(); })</c>
/// from their <c>Program.cs</c>.
/// </summary>
public static class DurableCommandsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the durable command bus + consumer + ledger over <typeparamref name="TContext"/>.
    /// Caller's <paramref name="configure"/> declares which commands opt in.
    /// </summary>
    public static IServiceCollection AddDurableCommandBus<TContext>(
        this IServiceCollection services,
        string moduleSlug,
        Action<DurableCommandsBuilder> configure)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleSlug);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<DurableCommandBusOptions>()
            .Configure(o => o.ModuleSlug = moduleSlug);

        var builder = new DurableCommandsBuilder(moduleSlug);
        configure(builder);

        services.AddSingleton<IDurableCommandCatalog>(_ => new DurableCommandCatalog(builder.Registrations));
        services.AddScoped<ICommandLedger, EfCommandLedger<TContext>>();
        services.AddSingleton<IDurableCommandBus, DurableCommandBus>();
        services.AddScoped<IConsumer<DurableCommandEnvelope>, DurableCommandConsumer<TContext>>();
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
