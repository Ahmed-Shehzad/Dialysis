using Dialysis.BuildingBlocks.DataProtection.DataSubjectRights;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.Lab.Persistence.DataSubjectRights;

/// <summary>
/// Lab contribution to the GDPR Art. 15 / 20 export pipeline. Structurally mirrors
/// <see cref="Erasure.LabPatientEraser"/>: reads the patient's <c>LabOrder</c> rows (the
/// LOINC-coded requested tests and resulted observations ride inline on the row) into
/// <see cref="DataSubjectResource"/> entries. Soft-deleted rows are excluded — after erasure they
/// are tombstones, not personal data undergoing processing. Rows serialize via
/// <see cref="DataSubjectExportJson"/>.
/// </summary>
public sealed class LabModuleDataExtractor : IModuleDataExtractor
{
    private readonly LabDbContext _ctx;

    public LabModuleDataExtractor(LabDbContext ctx) => _ctx = ctx;

    public string ModuleSlug => "lab";

    public async Task<IReadOnlyList<DataSubjectResource>> ExtractAsync(
        Guid patientId, CancellationToken cancellationToken)
    {
        var orders = await _ctx.LabOrders
            .AsNoTracking()
            .Where(o => o.PatientId == patientId && !o.IsDeleted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var resources = new List<DataSubjectResource>(orders.Count);
        foreach (var order in orders)
        {
            resources.Add(new DataSubjectResource(
                "LabOrder", order.Id.ToString(), DataSubjectExportJson.Serialize(order)));
        }
        return resources;
    }
}
