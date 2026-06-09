using Dialysis.BuildingBlocks.Transponder.Scheduling.Hangfire;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.DependencyInjection;

// Namespace deliberately omits the `Hangfire` segment so `using Hangfire;` binds to the global Hangfire
// namespace rather than this assembly's `...Scheduling.Hangfire`.
namespace Dialysis.BuildingBlocks.Transponder.Hosting;

/// <summary>
/// One-call host wiring for the Hangfire-backed Transponder scheduler. Registers Hangfire with
/// PostgreSQL storage (Hangfire owns its <c>hangfire</c> schema in the supplied database), the
/// background server that runs jobs, and the Hangfire <see cref="ITransponderMessageScheduler"/>.
/// </summary>
public static class HangfireTransponderHostExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds Hangfire (PostgreSQL storage + server) and the Hangfire-backed Transponder scheduler.
        /// <paramref name="connectionString"/> is the host's PostgreSQL connection string.
        /// </summary>
        public IServiceCollection AddTransponderHangfire(string connectionString)
        {
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(postgres => postgres.UseNpgsqlConnection(connectionString)));
            services.AddHangfireServer();
            services.AddTransponderHangfireScheduling();
            return services;
        }
    }
}
