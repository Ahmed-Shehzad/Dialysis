using Dialysis.BuildingBlocks.Transponder.Scheduling.Hangfire;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

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
    /// <summary>
    /// Advisory-lock key serializing Hangfire schema installation per database. Several hosts share
    /// one module database (module API + context BFF + the identity/portal BFFs on HIS, the admin
    /// BFF on HIE), and <c>Hangfire.PostgreSql</c>'s installer is not concurrency-safe on first
    /// boot — parallel installs race the Postgres catalog (e.g. <c>XX000: could not find tuple for
    /// constraint</c>) and the losing process dies with an unhandled startup exception. Distinct
    /// from the outbox relay's <c>AdvisoryLockKey</c> so the two never contend.
    /// </summary>
    public const long SchemaInstallAdvisoryLockKey = 730_415_523_891_218;

    private const string SchemaName = "hangfire";

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds Hangfire (PostgreSQL storage + server) and the Hangfire-backed Transponder scheduler.
        /// <paramref name="connectionString"/> is the host's PostgreSQL connection string. The
        /// <c>hangfire</c> schema is installed up front under a per-database advisory lock so
        /// replicas and co-tenant hosts booting concurrently serialize instead of crashing.
        /// </summary>
        public IServiceCollection AddTransponderHangfire(string connectionString)
        {
            PrepareSchemaSerialized(connectionString);

            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(
                    postgres => postgres.UseNpgsqlConnection(connectionString),
                    new PostgreSqlStorageOptions
                    {
                        // Installed by PrepareSchemaSerialized above; the storage itself must not
                        // re-run the installer or co-tenant hosts race it again.
                        PrepareSchemaIfNecessary = false,
                    }));
            services.AddHangfireServer();
            services.AddTransponderHangfireScheduling();
            return services;
        }
    }

    /// <summary>
    /// Runs <see cref="PostgreSqlObjectsInstaller"/> while holding a session advisory lock, retrying
    /// briefly so a host that boots while the database is still being provisioned (CI cold start)
    /// doesn't die on the first connection failure. The lock is released explicitly — returning a
    /// pooled connection does not release session advisory locks.
    /// </summary>
    private static void PrepareSchemaSerialized(string connectionString)
    {
        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var connection = new NpgsqlConnection(connectionString);
                connection.Open();
                using (var acquire = new NpgsqlCommand($"SELECT pg_advisory_lock({SchemaInstallAdvisoryLockKey})", connection))
                {
                    acquire.ExecuteNonQuery();
                }

                try
                {
                    PostgreSqlObjectsInstaller.Install(connection, SchemaName);
                }
                finally
                {
                    using var release = new NpgsqlCommand($"SELECT pg_advisory_unlock({SchemaInstallAdvisoryLockKey})", connection);
                    release.ExecuteNonQuery();
                }

                return;
            }
            catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException && attempt < maxAttempts)
            {
                Thread.Sleep(TimeSpan.FromSeconds(2 * attempt));
            }
        }
    }
}
