using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Scheduling.Quartz;

public static class QuartzTransponderSchedulingServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ITransponderMessageScheduler"/> backed by Quartz.NET. You must also register Quartz and the hosted scheduler
    /// (for example <c>services.AddQuartz(q =&gt; q.UseMicrosoftDependencyInjectionJobFactory());</c> and <c>services.AddQuartzHostedService(...)</c>).
    /// Recurring expressions use Quartz cron syntax.
    /// </summary>
    public static IServiceCollection AddTransponderQuartzScheduling(this IServiceCollection services)
    {
        services.RemoveDescriptorsFor(typeof(ITransponderMessageScheduler));
        services.AddTransient<TransponderQuartzPublishJob>();
        services.AddSingleton<ITransponderMessageScheduler, QuartzTransponderMessageScheduler>();
        return services;
    }
}
