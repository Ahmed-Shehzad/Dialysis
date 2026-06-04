using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfRaCapabilityCommandStore : IRaCapabilityCommandStore
{
    private readonly HisDbContext _db;
    public EfRaCapabilityCommandStore(HisDbContext db) => _db = db;
    public void AddWaitlistEntry(RaWaitlistEntry entry) => _db.RaWaitlistEntries.Add(entry);

    public async Task<bool> TryClearPatientAlertAsync(Guid alertId, DateTime clearedAtUtc, CancellationToken cancellationToken = default)
    {
        var a = await _db.RaPatientAlerts.FirstOrDefaultAsync(x => x.Id == alertId, cancellationToken).ConfigureAwait(false);
        if (a is null || a.ClearedAtUtc is not null)
            return false;
        a.ClearedAtUtc = clearedAtUtc;
        return true;
    }

    public void AddClinicalDecisionSupportEvaluation(RaClinicalDecisionSupportEvaluation evaluation) =>
        _db.RaClinicalDecisionSupportEvaluations.Add(evaluation);

    public void AddOrganizationalCommunication(RaOrgCommunication row) => _db.RaOrgCommunications.Add(row);

    public void AddAnalyticsExportJob(RaAnalyticsExportJob job) => _db.RaAnalyticsExportJobs.Add(job);

    public void AddEhrDocumentExchangeRecord(RaEhrDocumentExchangeRecord record) => _db.RaEhrDocumentExchangeRecords.Add(record);

    public async Task<bool> TryUpdateQualityWorkflowTaskStatusAsync(
        Guid taskId,
        string newStatusCode,
        CancellationToken cancellationToken = default)
    {
        var t = await _db.RaQualityWorkflowTasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken).ConfigureAwait(false);
        if (t is null)
            return false;

        var normalized = newStatusCode.Trim().ToLowerInvariant();
        t.StatusCode = normalized;
        t.ClosedAtUtc = normalized is "closed" or "cancelled" ? DateTime.UtcNow : null;
        return true;
    }

    public void AddSecurityMechanismHardening(RaSecurityMechanismHardening row) => _db.RaSecurityMechanismHardenings.Add(row);

    public void AddSpecialistEncounterRecord(RaSpecialistEncounterRecord record) => _db.RaSpecialistEncounterRecords.Add(record);

    public void AddResearchEducationActivity(RaResearchEducationActivity activity) => _db.RaResearchEducationActivities.Add(activity);

    public void AddFinancialErpLink(RaFinancialErpLink link) => _db.RaFinancialErpLinks.Add(link);

    public void AddMedicationDispensingRecord(RaMedicationDispensingRecord record) => _db.RaMedicationDispensingRecords.Add(record);
}
