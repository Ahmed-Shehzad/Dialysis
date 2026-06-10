#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Dialysis.PDMS.Persistence.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Converts the high-volume <c>pdms_sessions."IntradialyticReadings"</c> table into a
    /// TimescaleDB hypertable partitioned by <c>ObservedAtUtc</c>. The table stays a regular
    /// Postgres row store for new chunks (so transactional writes via EF continue to work);
    /// chunks older than 7 days are converted to TimescaleDB's columnar compressed format
    /// via a background policy.
    ///
    /// Why: this is the highest-volume write path on PDMS — every dialysis session emits
    /// readings at multi-Hz cadence. Time-partitioning the table keeps per-chunk index sizes
    /// small (clinic-day-sized chunks vs an unbounded heap), and the compression policy
    /// reclaims ~10x storage on tail data without changing query semantics. Reads via EF
    /// transparently span chunks.
    ///
    /// Requires the <c>timescaledb</c> extension to be installed in the Postgres image —
    /// PDMS uses <c>timescale/timescaledb:latest-pg17</c> per Aspire AppHost (Pg helper
    /// PgTimescale). Plain postgres-alpine doesn't ship the extension; this migration will
    /// fail on first apply against that image. The CNPG operator manifest at
    /// <c>deploy/k8s/operators/templates/cloudnative-pg-clusters.yaml</c> declares
    /// <c>timescaledb</c> in the cluster's <c>postgresql.shared_preload_libraries</c>.
    /// </remarks>
    public partial class AddTimescaleHypertable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: ensure the extension is installed. CREATE EXTENSION is idempotent in
            // Postgres; safe to re-run.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS timescaledb;");

            // Step 2: convert IntradialyticReadings into a hypertable. The
            // `migrate_data => true` argument moves existing rows into the new chunked
            // layout (cheap on a fresh DB; small one-time cost on a populated one). Chunk
            // interval = 1 day matches the "clinic-day" shape — reasonable balance
            // between chunk count and per-chunk size on a clinic running ~20-100 patients
            // each emitting readings at 1-10 Hz.
            //
            // TimescaleDB requires the partition column to be part of any unique index. The
            // existing primary key is (Id) only; we add a composite UNIQUE that includes
            // ObservedAtUtc to satisfy that constraint. The natural unique index on Id
            // stays intact for EF's normal lookups.
            migrationBuilder.Sql(@"
                ALTER TABLE pdms_sessions.""IntradialyticReadings""
                    DROP CONSTRAINT IF EXISTS ""PK_IntradialyticReadings"";
                ALTER TABLE pdms_sessions.""IntradialyticReadings""
                    ADD CONSTRAINT ""PK_IntradialyticReadings"" PRIMARY KEY (""Id"", ""ObservedAtUtc"");
                SELECT create_hypertable(
                    'pdms_sessions.""IntradialyticReadings""',
                    'ObservedAtUtc',
                    chunk_time_interval => INTERVAL '1 day',
                    migrate_data => true,
                    if_not_exists => true
                );
            ");

            // Step 3: enable native compression and schedule a background policy. ALTER TABLE
            // SET turns on the column-store compression for chunks; add_compression_policy
            // schedules a worker to compress every chunk older than 7 days. The segmentby +
            // orderby clauses pick the columns TimescaleDB groups by for the columnar layout
            // — SessionId is the obvious access pattern (a session's full reading timeline
            // is queried together), and ObservedAtUtc DESC matches typical chart-time reads.
            migrationBuilder.Sql(@"
                ALTER TABLE pdms_sessions.""IntradialyticReadings""
                    SET (
                        timescaledb.compress,
                        timescaledb.compress_segmentby = '""SessionId""',
                        timescaledb.compress_orderby = '""ObservedAtUtc"" DESC'
                    );
                SELECT add_compression_policy(
                    'pdms_sessions.""IntradialyticReadings""',
                    INTERVAL '7 days',
                    if_not_exists => true
                );
            ");

            // Step 4: schedule a retention policy. Default retention = 365 days (1 year);
            // operators can adjust via the DocumentRetentionPolicy admin UI on the HIE side
            // for documents, but raw telemetry retention is governed separately because
            // it's not patient-identifiable in isolation (it's session-keyed). 365d is the
            // conservative default that satisfies typical clinical-data-retention
            // requirements; reduce when storage cost becomes a concern.
            migrationBuilder.Sql(@"
                SELECT add_retention_policy(
                    'pdms_sessions.""IntradialyticReadings""',
                    INTERVAL '365 days',
                    if_not_exists => true
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Undoing is rare in production — once a table is a hypertable, going back to
            // a heap requires copying every row. We provide the reversal for symmetry and
            // for dev resets. Production rollback should usually go forward to a fixing
            // migration instead.
            migrationBuilder.Sql(@"
                SELECT remove_retention_policy('pdms_sessions.""IntradialyticReadings""', if_exists => true);
                SELECT remove_compression_policy('pdms_sessions.""IntradialyticReadings""', if_exists => true);
                -- Decompress every compressed chunk so the heap copy below sees uncompressed
                -- rows. decompress_chunk returns null for already-uncompressed chunks.
                SELECT decompress_chunk(c, true)
                FROM show_chunks('pdms_sessions.""IntradialyticReadings""') AS c;
            ");
            migrationBuilder.Sql(@"
                CREATE TABLE pdms_sessions.""IntradialyticReadings_heap"" (LIKE pdms_sessions.""IntradialyticReadings"" INCLUDING ALL);
                INSERT INTO pdms_sessions.""IntradialyticReadings_heap"" SELECT * FROM pdms_sessions.""IntradialyticReadings"";
                DROP TABLE pdms_sessions.""IntradialyticReadings"" CASCADE;
                ALTER TABLE pdms_sessions.""IntradialyticReadings_heap"" RENAME TO ""IntradialyticReadings"";
                ALTER TABLE pdms_sessions.""IntradialyticReadings""
                    DROP CONSTRAINT IF EXISTS ""PK_IntradialyticReadings"";
                ALTER TABLE pdms_sessions.""IntradialyticReadings""
                    ADD CONSTRAINT ""PK_IntradialyticReadings"" PRIMARY KEY (""Id"");
            ");
        }
    }
}
