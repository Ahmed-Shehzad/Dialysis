using System.Globalization;
using System.Text;
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
/// PostgreSQL storage, the background server that runs jobs, and the Hangfire
/// <see cref="ITransponderMessageScheduler"/>. Every host gets its <b>own schema</b> (and
/// therefore its own job queue and its own server polling only that queue) inside the database
/// it already uses — co-tenant hosts on one module database (module API + context BFF + the
/// identity/portal BFFs on HIS, the admin BFF on HIE) never execute each other's jobs, and no
/// dedicated Hangfire database is needed.
/// </summary>
public static class HangfireTransponderHostExtensions
{
    /// <summary>
    /// Advisory-lock key serializing Hangfire schema installation per database server. Even with
    /// per-host schemas the installer needs serializing: multiple replicas of the <b>same</b> host
    /// install the <b>same</b> schema concurrently on first boot, and <c>Hangfire.PostgreSql</c>'s
    /// installer is not concurrency-safe (parallel installs race the Postgres catalog, e.g.
    /// <c>XX000: could not find tuple for constraint</c>, killing the loser with an unhandled
    /// startup exception). Distinct from the outbox relay's <c>AdvisoryLockKey</c> so the two
    /// never contend.
    /// </summary>
    public const long SchemaInstallAdvisoryLockKey = 730_415_523_891_218;

    /// <summary>Fallback schema when no host-specific name is supplied (tests, ad-hoc hosts).</summary>
    private const string DefaultSchemaName = "hangfire";

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds Hangfire (PostgreSQL storage + server) and the Hangfire-backed Transponder scheduler.
        /// <paramref name="connectionString"/> is the host's PostgreSQL connection string;
        /// <paramref name="schemaName"/> is the host's own Hangfire schema (pass a per-host name like
        /// <c>hangfire_his_bff</c> — co-tenant hosts sharing a database must not share a schema, or
        /// they dequeue each other's jobs). The schema is installed up front under a per-database
        /// advisory lock so replicas booting concurrently serialize instead of crashing.
        /// </summary>
        public IServiceCollection AddTransponderHangfire(string connectionString, string? schemaName = null)
        {
            var schema = SanitizeSchemaName(schemaName);
            PrepareSchemaSerialized(connectionString, schema);

            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(
                    postgres => postgres.UseNpgsqlConnection(connectionString),
                    new PostgreSqlStorageOptions
                    {
                        SchemaName = schema,
                        // Installed by PrepareSchemaSerialized above; the storage itself must not
                        // re-run the installer or concurrent replicas race it again.
                        PrepareSchemaIfNecessary = false,
                    }));
            services.AddHangfireServer();
            services.AddTransponderHangfireScheduling();
            return services;
        }
    }

    /// <summary>
    /// Lowercases and collapses anything outside <c>[a-z0-9_]</c> to <c>_</c> so slugs like
    /// <c>smartconnect-bff</c> become valid PostgreSQL schema identifiers; truncates to the
    /// 63-byte identifier limit. Null/blank falls back to <see cref="DefaultSchemaName"/>.
    /// </summary>
    private static string SanitizeSchemaName(string? schemaName)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
        {
            return DefaultSchemaName;
        }

        var builder = new StringBuilder(schemaName.Length);
        foreach (var ch in schemaName.Trim().ToLower(CultureInfo.InvariantCulture))
        {
            builder.Append(ch is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_' ? ch : '_');
        }

        var sanitized = builder.ToString();
        return sanitized.Length <= 63 ? sanitized : sanitized[..63];
    }

    /// <summary>
    /// Runs <see cref="PostgreSqlObjectsInstaller"/> while holding a session advisory lock, retrying
    /// so a host that boots while the database is still being provisioned (CI cold start) doesn't
    /// die on the first connection failure. The acquire command waits indefinitely
    /// (<c>CommandTimeout = 0</c>) — hosts queue behind a slow first install instead of tripping
    /// Npgsql's default 30 s timeout. The lock is released explicitly — returning a pooled
    /// connection does not release session advisory locks.
    /// </summary>
    private static void PrepareSchemaSerialized(string connectionString, string schemaName)
    {
        const int maxAttempts = 8;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var connection = new NpgsqlConnection(connectionString);
                connection.Open();
                using (var acquire = new NpgsqlCommand($"SELECT pg_advisory_lock({SchemaInstallAdvisoryLockKey})", connection))
                {
                    acquire.CommandTimeout = 0;
                    acquire.ExecuteNonQuery();
                }

                try
                {
                    PostgreSqlObjectsInstaller.Install(connection, schemaName);
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
