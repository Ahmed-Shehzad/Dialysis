using System.Text.Json;
using Dialysis.Analytics.Configuration;
using Dialysis.Analytics.Features.Cohorts;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Dialysis.Analytics.Data;

/// <summary>PostgreSQL-backed store for saved cohorts. Uses saved_cohorts table.</summary>
public sealed class PostgresSavedCohortStore : ISavedCohortStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly string _connectionString;

    public PostgresSavedCohortStore(IOptions<AnalyticsOptions> options)
    {
        _connectionString = options.Value.ConnectionString ?? throw new InvalidOperationException("ConnectionStrings:Analytics is required for PostgresSavedCohortStore");
    }

    public async Task EnsureTableAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(@"
            CREATE TABLE IF NOT EXISTS saved_cohorts (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                criteria JSONB NOT NULL DEFAULT '{}',
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                tenant_id TEXT
            )", conn);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<SavedCohort?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(
            "SELECT id, name, criteria, created_at, updated_at FROM saved_cohorts WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        var criteriaJson = reader.GetString(2);
        var criteria = JsonSerializer.Deserialize<CohortCriteria>(criteriaJson ?? "{}", JsonOptions) ?? new CohortCriteria();

        return new SavedCohort
        {
            Id = reader.GetString(0),
            Name = reader.GetString(1),
            Criteria = criteria,
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(3),
            UpdatedAt = reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4)
        };
    }

    public async Task<IReadOnlyList<SavedCohort>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(
            "SELECT id, name, criteria, created_at, updated_at FROM saved_cohorts ORDER BY created_at DESC", conn);

        var list = new List<SavedCohort>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var criteriaJson = reader.GetString(2);
            var criteria = JsonSerializer.Deserialize<CohortCriteria>(criteriaJson ?? "{}", JsonOptions) ?? new CohortCriteria();
            list.Add(new SavedCohort
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                Criteria = criteria,
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(3),
                UpdatedAt = reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4)
            });
        }
        return list;
    }

    public async Task<SavedCohort> SaveAsync(SavedCohort cohort, CancellationToken cancellationToken = default)
    {
        var id = string.IsNullOrEmpty(cohort.Id) ? Ulid.NewUlid().ToString() : cohort.Id;
        var now = DateTimeOffset.UtcNow;
        var criteriaJson = JsonSerializer.Serialize(cohort.Criteria, JsonOptions);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO saved_cohorts (id, name, criteria, created_at, updated_at)
            VALUES (@id, @name, @criteria::jsonb, @created_at, @updated_at)
            ON CONFLICT (id) DO UPDATE SET
                name = EXCLUDED.name,
                criteria = EXCLUDED.criteria,
                updated_at = EXCLUDED.updated_at", conn);

        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("name", cohort.Name);
        cmd.Parameters.AddWithValue("criteria", criteriaJson);
        cmd.Parameters.AddWithValue("created_at", cohort.CreatedAt != default ? cohort.CreatedAt : now);
        cmd.Parameters.AddWithValue("updated_at", now);

        await cmd.ExecuteNonQueryAsync(cancellationToken);

        var criteria = JsonSerializer.Deserialize<CohortCriteria>(criteriaJson, JsonOptions) ?? cohort.Criteria;
        return new SavedCohort
        {
            Id = id,
            Name = cohort.Name,
            Criteria = criteria,
            CreatedAt = cohort.CreatedAt != default ? cohort.CreatedAt : now,
            UpdatedAt = now
        };
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("DELETE FROM saved_cohorts WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", id);
        return await cmd.ExecuteNonQueryAsync(cancellationToken) > 0;
    }
}
