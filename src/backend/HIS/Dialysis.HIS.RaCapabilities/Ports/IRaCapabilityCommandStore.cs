using Dialysis.HIS.RaCapabilities.Domain;

namespace Dialysis.HIS.RaCapabilities.Ports;

public interface IRaCapabilityCommandStore
{
    void AddWaitlistEntry(RaWaitlistEntry entry);

    Task<bool> TryClearPatientAlertAsync(Guid alertId, DateTime clearedAtUtc, CancellationToken cancellationToken = default);

    void AddClinicalDecisionSupportEvaluation(RaClinicalDecisionSupportEvaluation evaluation);

    void AddOrganizationalCommunication(RaOrgCommunication row);

    void AddAnalyticsExportJob(RaAnalyticsExportJob job);

    void AddEhrDocumentExchangeRecord(RaEhrDocumentExchangeRecord record);

    Task<bool> TryUpdateQualityWorkflowTaskStatusAsync(Guid taskId, string newStatusCode, CancellationToken cancellationToken = default);

    void AddSecurityMechanismHardening(RaSecurityMechanismHardening row);

    void AddSpecialistEncounterRecord(RaSpecialistEncounterRecord record);

    void AddResearchEducationActivity(RaResearchEducationActivity activity);
}
