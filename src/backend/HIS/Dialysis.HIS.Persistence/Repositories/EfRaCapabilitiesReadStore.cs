using Dialysis.HIS.RaCapabilities.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfRaCapabilitiesReadStore : IRaCapabilitiesReadStore
{
    private readonly HisDbContext _db;
    public EfRaCapabilitiesReadStore(HisDbContext db) => _db = db;
    public async Task<IReadOnlyList<RaOrgCommunicationRow>> ListOrganizationalCommunicationsAsync(
        CancellationToken cancellationToken = default) =>
        await _db.RaOrgCommunications.AsNoTracking()
            .OrderByDescending(x => x.SentAtUtc)
            .Select(x => new RaOrgCommunicationRow(x.Id, x.ThreadCode, x.Subject, x.Body, x.SentAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<RaQualityWorkflowTaskRow>> ListQualityWorkflowTasksAsync(
        CancellationToken cancellationToken = default) =>
        await _db.RaQualityWorkflowTasks.AsNoTracking()
            .OrderByDescending(x => x.OpenedAtUtc)
            .Select(x => new RaQualityWorkflowTaskRow(x.Id, x.TaskCode, x.Title, x.StatusCode, x.OpenedAtUtc, x.ClosedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<RaFinancialErpLinkRow>> ListFinancialErpLinksAsync(
        CancellationToken cancellationToken = default) =>
        await _db.RaFinancialErpLinks.AsNoTracking()
            .OrderBy(x => x.SystemCode)
            .Select(x => new RaFinancialErpLinkRow(x.Id, x.SystemCode, x.LastHandshakeAtUtc, x.StatusCode))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<RaWaitlistEntryRow>> ListWaitlistEntriesAsync(
        CancellationToken cancellationToken = default) =>
        await _db.RaWaitlistEntries.AsNoTracking()
            .OrderBy(x => x.EnqueuedAtUtc)
            .Select(x => new RaWaitlistEntryRow(x.Id, x.PatientId, x.ResourceKindCode, x.Notes, x.RequestedNotBeforeUtc, x.EnqueuedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<RaEhrDocumentExchangeRow>> ListEhrDocumentExchangesAsync(
        CancellationToken cancellationToken = default) =>
        await _db.RaEhrDocumentExchangeRecords.AsNoTracking()
            .OrderByDescending(x => x.ExchangedAtUtc)
            .Select(x => new RaEhrDocumentExchangeRow(x.Id, x.PatientId, x.DocumentTypeCode, x.ExternalSystemCode, x.ExternalUri, x.ExchangedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<RaPatientAlertRow>> ListPatientAlertsAsync(
        CancellationToken cancellationToken = default) =>
        await _db.RaPatientAlerts.AsNoTracking()
            .OrderByDescending(x => x.RaisedAtUtc)
            .Select(x => new RaPatientAlertRow(x.Id, x.PatientId, x.RuleCode, x.Severity, x.Message, x.RaisedAtUtc, x.ClearedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<RaMedicationDispensingRow>> ListMedicationDispensingRecordsAsync(
        CancellationToken cancellationToken = default) =>
        await _db.RaMedicationDispensingRecords.AsNoTracking()
            .OrderByDescending(x => x.DispensedAtUtc)
            .Select(x => new RaMedicationDispensingRow(x.Id, x.MedicationOrderId, x.BarcodeToken, x.DispensedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<RaClinicalDecisionSupportRow>> ListClinicalDecisionSupportEvaluationsAsync(
        CancellationToken cancellationToken = default) =>
        await _db.RaClinicalDecisionSupportEvaluations.AsNoTracking()
            .OrderByDescending(x => x.EvaluatedAtUtc)
            .Select(x => new RaClinicalDecisionSupportRow(x.Id, x.PatientId, x.ChecksAppliedJson, x.SafeToProceed, x.EvaluatedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<RaAnalyticsExportJobRow>> ListAnalyticsExportJobsAsync(
        CancellationToken cancellationToken = default) =>
        await _db.RaAnalyticsExportJobs.AsNoTracking()
            .OrderByDescending(x => x.RequestedAtUtc)
            .Select(x => new RaAnalyticsExportJobRow(x.Id, x.PipelineCode, x.RequestedAtUtc, x.StatusCode))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<RaFullTextSearchEntryRow>> ListFullTextSearchEntriesAsync(
        string? searchTextContains,
        CancellationToken cancellationToken = default)
    {
        var q = _db.RaFullTextSearchEntries.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(searchTextContains))
        {
            var needle = searchTextContains.Trim();
            q = q.Where(x => x.SearchText.Contains(needle));
        }

        return await q.OrderByDescending(x => x.IndexedAtUtc)
            .Select(x => new RaFullTextSearchEntryRow(x.Id, x.CorpusCode, x.ExternalId, x.SearchText, x.IndexedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RaSecurityMechanismRow>> ListSecurityMechanismHardeningsAsync(
        CancellationToken cancellationToken = default) =>
        await _db.RaSecurityMechanismHardenings.AsNoTracking()
            .OrderByDescending(x => x.AssessedAtUtc)
            .Select(x => new RaSecurityMechanismRow(x.Id, x.MechanismCode, x.AppliedLevel, x.Notes, x.AssessedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<RaSpecialistEncounterRow>> ListSpecialistEncountersAsync(
        CancellationToken cancellationToken = default) =>
        await _db.RaSpecialistEncounterRecords.AsNoTracking()
            .OrderByDescending(x => x.RecordedAtUtc)
            .Select(x => new RaSpecialistEncounterRow(x.Id, x.PatientId, x.SpecialtyCode, x.ExternalSystemCode, x.Summary, x.RecordedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<RaResearchEducationActivityRow>> ListResearchEducationActivitiesAsync(
        CancellationToken cancellationToken = default) =>
        await _db.RaResearchEducationActivities.AsNoTracking()
            .OrderByDescending(x => x.RecordedAtUtc)
            .Select(x => new RaResearchEducationActivityRow(x.Id, x.ActivityKindCode, x.Title, x.ExternalReference, x.RecordedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
}
