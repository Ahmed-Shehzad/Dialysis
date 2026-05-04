using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;

namespace Dialysis.BuildingBlocks.Transponder.Scheduling.TickerQ;

public static class TickerQTransponderSchedulingServiceCollectionExtensions
{
    /// <summary>
    /// Registers TickerQ (when not already present), indexes <see cref="TransponderTickerQPublishJobs"/> from this assembly, and registers
    /// <see cref="ITransponderMessageScheduler"/>. Use <c>app.UseTickerQ()</c> in the web pipeline. Prefer a single call to this overload rather than
    /// separate <c>AddTickerQ</c> unless you need advanced configuration—in that case use <see cref="ConfigureTransponderTickerQDiscovery"/> inside your
    /// <c>AddTickerQ</c> callback and call <see cref="AddTransponderTickerQSchedulerOnly"/> for the scheduler registration.
    /// </summary>
    public static IServiceCollection AddTransponderTickerQScheduling(
        this IServiceCollection services,
        Action<TickerOptionsBuilder<TimeTickerEntity, CronTickerEntity>>? configureTickerQ = null)
    {
        services.RemoveDescriptorsFor(typeof(ITransponderMessageScheduler));
        services.AddTickerQ<TimeTickerEntity, CronTickerEntity>(b =>
        {
            b.AddTickerQDiscovery(new[] { Assembly.GetExecutingAssembly() });
            configureTickerQ?.Invoke(b);
        });
        services.AddSingleton<TransponderTickerQPublishJobs>();
        services.AddSingleton<ITransponderMessageScheduler, TickerQTransponderMessageScheduler>();
        return services;
    }

    /// <summary>Adds Transponder TickerQ function discovery to an existing <c>AddTickerQ</c> configuration.</summary>
    public static TickerOptionsBuilder<TimeTickerEntity, CronTickerEntity> ConfigureTransponderTickerQDiscovery(
        this TickerOptionsBuilder<TimeTickerEntity, CronTickerEntity> builder) =>
        builder.AddTickerQDiscovery(new[] { Assembly.GetExecutingAssembly() });

    /// <summary>Registers only <see cref="ITransponderMessageScheduler"/> and job services (TickerQ must already be configured).</summary>
    public static IServiceCollection AddTransponderTickerQSchedulerOnly(this IServiceCollection services)
    {
        services.RemoveDescriptorsFor(typeof(ITransponderMessageScheduler));
        services.AddSingleton<TransponderTickerQPublishJobs>();
        services.AddSingleton<ITransponderMessageScheduler, TickerQTransponderMessageScheduler>();
        return services;
    }
}
