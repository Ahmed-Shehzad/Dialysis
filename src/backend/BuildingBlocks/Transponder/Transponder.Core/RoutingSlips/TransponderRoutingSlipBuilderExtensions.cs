using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>Registers durable routing slip activities and the continue consumer.</summary>
public static class TransponderRoutingSlipBuilderExtensions
{
    /// <summary>
    /// Registers a scoped <typeparamref name="TActivity"/> keyed by <paramref name="activityName"/> (defaults to the type name).
    /// Registers <see cref="ITransponderRoutingSlipStarter"/> and <see cref="ITransponderSagaStore"/> defaults when missing, and a single consumer for <see cref="TransponderRoutingSlipContinue"/> the first time routing slips are used on this service collection.
    /// </summary>
    public static TransponderBuilder AddRoutingSlipActivity<TActivity>(this TransponderBuilder builder, string? activityName = null)
        where TActivity : class, IRoutingSlipActivity
    {
        ArgumentNullException.ThrowIfNull(builder);
        var name = string.IsNullOrWhiteSpace(activityName) ? typeof(TActivity).Name : activityName.Trim();

        EnsureRoutingSlipHost(builder);

        builder.Services.Configure<TransponderRoutingSlipOptions>(o => o.ActivitiesByName[name] = typeof(TActivity));
        builder.Services.TryAddScoped<TActivity>();

        return builder;
    }

    private static void EnsureRoutingSlipHost(TransponderBuilder builder)
    {
        if (builder.Services.Any(static d => d.ServiceType == typeof(TransponderRoutingSlipModuleMarker)))
            return;

        builder.Services.AddSingleton<TransponderRoutingSlipModuleMarker>();
        builder.Services.TryAddSingleton<ITransponderSagaStore, InMemoryTransponderSagaStore>();
        builder.Services.TryAddSingleton<ITransponderRoutingSlipStarter, TransponderRoutingSlipStarter>();
        builder.Services.TryAddSingleton<TransponderRoutingSlipEventPublisher>();
        builder.Services.AddOptions<TransponderRoutingSlipOptions>();
        builder.AddConsumer<TransponderRoutingSlipContinue, TransponderRoutingSlipContinueConsumer>();
        TransponderRoutingSlipDiscardingEventConsumers.Register(builder);
    }
}
