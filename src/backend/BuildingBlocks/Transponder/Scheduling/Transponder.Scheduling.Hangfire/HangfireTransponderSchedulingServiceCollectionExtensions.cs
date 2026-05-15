using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Scheduling.Hangfire;

public static class HangfireTransponderSchedulingServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="ITransponderMessageScheduler"/> backed by Hangfire. Call after Hangfire client registration
        /// (<c>AddHangfire</c> provides <c>IBackgroundJobClient</c>; also add <c>AddHangfireServer</c> so jobs run).
        /// Recurring cron uses Hangfire's NCrontab dialect.
        /// </summary>
        public IServiceCollection AddTransponderHangfireScheduling()
        {
            services.RemoveDescriptorsFor(typeof(ITransponderMessageScheduler));
            services.AddTransient<TransponderHangfirePublishJob>();
            services.AddSingleton<ITransponderMessageScheduler, HangfireTransponderMessageScheduler>();
            return services;
        }
    }
}
