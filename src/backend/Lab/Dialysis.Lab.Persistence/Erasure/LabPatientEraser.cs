using Dialysis.BuildingBlocks.DataProtection.Erasure;
using Dialysis.DomainDrivenDesign.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dialysis.Lab.Persistence.Erasure;

/// <summary>
/// Lab contribution to the GDPR Art. 17 erasure pipeline. The Lab bounded context holds exactly
/// one patient-linked aggregate — <c>LabOrder</c> (the requested tests and resulted observations
/// ride inline on the row as JSON, so erasing the order erases them too). The aggregate inherits
/// the <see cref="Audit"/> primitive, so erasure is a soft delete via <c>ExecuteUpdateAsync</c>
/// on <c>IsDeleted</c>/<c>DeletedAt</c>/<c>DeletedBy</c> — one round-trip, mirroring the HIS eraser.
///
/// Idempotent: re-running on a patient with nothing left returns zero. Records erased are counted
/// per CLR type so the composite audit row on <c>ErasureRequest.ExecutionLog</c> answers "show me
/// what was deleted" without replaying the operation.
/// </summary>
public sealed class LabPatientEraser : IPatientEraser
{
    private readonly LabDbContext _ctx;
    private readonly TimeProvider _clock;
    private readonly ILogger<LabPatientEraser> _logger;

    public LabPatientEraser(
        LabDbContext ctx,
        TimeProvider clock,
        ILogger<LabPatientEraser> logger)
    {
        _ctx = ctx;
        _clock = clock;
        _logger = logger;
    }

    public string ModuleSlug => "lab";

    public async Task<PatientErasureResult> EraseAsync(
        Guid patientId,
        string approvedBy,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvedBy);
        var now = _clock.GetUtcNow().UtcDateTime;
        var byCategory = new Dictionary<string, int>(StringComparer.Ordinal);

        await SoftDeleteAsync(
            _ctx.LabOrders.Where(o => o.PatientId == patientId),
            "LabOrder", now, approvedBy, byCategory, cancellationToken).ConfigureAwait(false);

        var total = byCategory.Values.Sum();
        if (total > 0)
        {
            _logger.LogInformation(
                "DSR Art. 17 erasure (Lab) — patient {PatientId}: {Total} row(s) across {Categories} type(s) by {ApprovedBy}.",
                patientId, total, byCategory.Count, approvedBy);
        }
        return new PatientErasureResult(total, byCategory);
    }

    private static async Task SoftDeleteAsync<T>(
        IQueryable<T> query,
        string category,
        DateTime now,
        string approvedBy,
        Dictionary<string, int> byCategory,
        CancellationToken cancellationToken)
        where T : Audit
    {
        var affected = await query
            .Where(e => !e.IsDeleted)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(e => e.IsDeleted, true)
                    .SetProperty(e => e.DeletedAt, now)
                    .SetProperty(e => e.DeletedBy, approvedBy),
                cancellationToken)
            .ConfigureAwait(false);
        if (affected > 0)
            byCategory[category] = affected;
    }
}
