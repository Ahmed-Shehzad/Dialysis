using Dialysis.BuildingBlocks.Transponder.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

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
            // Hosts that wire Hangfire without a full Transponder bus (e.g. BFFs that don't call
            // AddModuleBffEvents) must survive Development's ValidateOnBuild: the serializer gets
            // the same default the core bus uses, and the publish job resolves its bus through a
            // factory so the missing ITransponderBus only surfaces if such a host actually
            // schedules a message (a programming error), not at builder.Build().
            services.TryAddSingleton<IMessageSerializer, SystemTextJsonMessageSerializer>();
            services.RemoveDescriptorsFor(typeof(ITransponderMessageScheduler));
            services.AddTransient(provider => new TransponderHangfirePublishJob(
                provider.GetRequiredService<ITransponderBus>(),
                provider.GetRequiredService<IMessageSerializer>(),
                provider.GetRequiredService<ILogger<TransponderHangfirePublishJob>>()));
            services.AddSingleton<ITransponderMessageScheduler, HangfireTransponderMessageScheduler>();
            return services;
        }
    }
}
