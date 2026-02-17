using Dialysis.Domain.Aggregates;
using Dialysis.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Persistence.Queries;

/// <summary>
/// Compiled queries for hot paths (single-item lookups). Use in read-only query handlers.
/// Collection queries use DbContext directly with AsNoTracking() in handlers.
/// </summary>
public static class CompiledQueries
{
    public static readonly Func<DialysisDbContext, string, string, Task<Session?>> GetSessionById =
        EF.CompileAsyncQuery((DialysisDbContext db, string tenantId, string sessionId) =>
            db.Sessions.AsNoTracking().FirstOrDefault(s => s.TenantId.Value == tenantId && s.Id.ToString() == sessionId));

    public static readonly Func<DialysisDbContext, string, string, Task<Patient?>> GetPatientById =
        EF.CompileAsyncQuery((DialysisDbContext db, string tenantId, string logicalId) =>
            db.Patients.AsNoTracking().FirstOrDefault(p => p.TenantId.Value == tenantId && p.LogicalId.Value == logicalId));

    public static readonly Func<DialysisDbContext, string, string, Task<bool>> PatientExists =
        EF.CompileAsyncQuery((DialysisDbContext db, string tenantId, string logicalId) =>
            db.Patients.AsNoTracking().Any(p => p.TenantId.Value == tenantId && p.LogicalId.Value == logicalId));

    public static readonly Func<DialysisDbContext, string, string, Task<Observation?>> GetObservationById =
        EF.CompileAsyncQuery((DialysisDbContext db, string tenantId, string observationId) =>
            db.Observations.AsNoTracking().FirstOrDefault(o => o.TenantId.Value == tenantId && o.Id.ToString() == observationId));

    public static readonly Func<DialysisDbContext, string, Ulid, Task<Condition?>> GetConditionById =
        EF.CompileAsyncQuery((DialysisDbContext db, string tenantId, Ulid id) =>
            db.Conditions.AsNoTracking().FirstOrDefault(c => c.TenantId.Value == tenantId && c.Id == id));

    public static readonly Func<DialysisDbContext, string, Ulid, Task<EpisodeOfCare?>> GetEpisodeOfCareById =
        EF.CompileAsyncQuery((DialysisDbContext db, string tenantId, Ulid id) =>
            db.EpisodeOfCare.AsNoTracking().FirstOrDefault(e => e.TenantId.Value == tenantId && e.Id == id));
}
