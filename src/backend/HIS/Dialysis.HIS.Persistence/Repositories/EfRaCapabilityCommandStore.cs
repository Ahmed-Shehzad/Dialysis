using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfRaCapabilityCommandStore(HisDbContext db) : IRaCapabilityCommandStore
{
    public void AddWaitlistEntry(RaWaitlistEntry entry) => db.RaWaitlistEntries.Add(entry);

    public async Task<bool> TryClearPatientAlertAsync(Guid alertId, DateTime clearedAtUtc, CancellationToken cancellationToken = default)
    {
        var a = await db.RaPatientAlerts.FirstOrDefaultAsync(x => x.Id == alertId, cancellationToken).ConfigureAwait(false);
        if (a is null || a.ClearedAtUtc is not null)
            return false;
        a.ClearedAtUtc = clearedAtUtc;
        return true;
    }

    public void AddClinicalDecisionSupportEvaluation(RaClinicalDecisionSupportEvaluation evaluation) =>
        db.RaClinicalDecisionSupportEvaluations.Add(evaluation);

    public void AddOrganizationalCommunication(RaOrgCommunication row) => db.RaOrgCommunications.Add(row);

    public void AddAnalyticsExportJob(RaAnalyticsExportJob job) => db.RaAnalyticsExportJobs.Add(job);

    public void AddEhrDocumentExchangeRecord(RaEhrDocumentExchangeRecord record) => db.RaEhrDocumentExchangeRecords.Add(record);

    public async Task<bool> TryUpdateQualityWorkflowTaskStatusAsync(
        Guid taskId,
        string newStatusCode,
        CancellationToken cancellationToken = default)
    {
        var t = await db.RaQualityWorkflowTasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken).ConfigureAwait(false);
        if (t is null)
            return false;

        var normalized = newStatusCode.Trim().ToLowerInvariant();
        t.StatusCode = normalized;
        t.ClosedAtUtc = normalized is "closed" or "cancelled" ? DateTime.UtcNow : null;
        return true;
    }

    public void AddSecurityMechanismHardening(RaSecurityMechanismHardening row) => db.RaSecurityMechanismHardenings.Add(row);

    public void AddSpecialistEncounterRecord(RaSpecialistEncounterRecord record) => db.RaSpecialistEncounterRecords.Add(record);

    public void AddResearchEducationActivity(RaResearchEducationActivity activity) => db.RaResearchEducationActivities.Add(activity);

    public void AddFinancialErpLink(RaFinancialErpLink link) => db.RaFinancialErpLinks.Add(link);

    public void AddMedicationDispensingRecord(RaMedicationDispensingRecord record) => db.RaMedicationDispensingRecords.Add(record);
}
