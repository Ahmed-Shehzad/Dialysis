namespace Dialysis.HIS.RaCapabilities.Ports;

public interface IRaCapabilitiesReadStore
{
    Task<IReadOnlyList<RaOrgCommunicationRow>> ListOrganizationalCommunicationsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaQualityWorkflowTaskRow>> ListQualityWorkflowTasksAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaFinancialErpLinkRow>> ListFinancialErpLinksAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaWaitlistEntryRow>> ListWaitlistEntriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaEhrDocumentExchangeRow>> ListEhrDocumentExchangesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaPatientAlertRow>> ListPatientAlertsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaMedicationDispensingRow>> ListMedicationDispensingRecordsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaClinicalDecisionSupportRow>> ListClinicalDecisionSupportEvaluationsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaAnalyticsExportJobRow>> ListAnalyticsExportJobsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaFullTextSearchEntryRow>> ListFullTextSearchEntriesAsync(
        string? searchTextContains,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaSecurityMechanismRow>> ListSecurityMechanismHardeningsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaSpecialistEncounterRow>> ListSpecialistEncountersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaResearchEducationActivityRow>> ListResearchEducationActivitiesAsync(CancellationToken cancellationToken = default);
}

public sealed record RaOrgCommunicationRow(Guid Id, string ThreadCode, string Subject, string Body, DateTime SentAtUtc);

public sealed record RaQualityWorkflowTaskRow(
    Guid Id,
    string TaskCode,
    string Title,
    string StatusCode,
    DateTime OpenedAtUtc,
    DateTime? ClosedAtUtc);

public sealed record RaFinancialErpLinkRow(Guid Id, string SystemCode, DateTime? LastHandshakeAtUtc, string StatusCode);

public sealed record RaWaitlistEntryRow(
    Guid Id,
    Guid PatientId,
    string ResourceKindCode,
    string Notes,
    DateTime RequestedNotBeforeUtc,
    DateTime EnqueuedAtUtc);

public sealed record RaEhrDocumentExchangeRow(
    Guid Id,
    Guid PatientId,
    string DocumentTypeCode,
    string ExternalSystemCode,
    string ExternalUri,
    DateTime ExchangedAtUtc);

public sealed record RaPatientAlertRow(
    Guid Id,
    Guid PatientId,
    string RuleCode,
    string Severity,
    string Message,
    DateTime RaisedAtUtc,
    DateTime? ClearedAtUtc);

public sealed record RaMedicationDispensingRow(Guid Id, Guid MedicationOrderId, string BarcodeToken, DateTime DispensedAtUtc);

public sealed record RaClinicalDecisionSupportRow(
    Guid Id,
    Guid PatientId,
    string ChecksAppliedJson,
    bool SafeToProceed,
    DateTime EvaluatedAtUtc);

public sealed record RaAnalyticsExportJobRow(Guid Id, string PipelineCode, DateTime RequestedAtUtc, string StatusCode);

public sealed record RaFullTextSearchEntryRow(Guid Id, string CorpusCode, string ExternalId, string SearchText, DateTime IndexedAtUtc);

public sealed record RaSecurityMechanismRow(Guid Id, string MechanismCode, string AppliedLevel, string Notes, DateTime AssessedAtUtc);

public sealed record RaSpecialistEncounterRow(
    Guid Id,
    Guid PatientId,
    string SpecialtyCode,
    string ExternalSystemCode,
    string Summary,
    DateTime RecordedAtUtc);

public sealed record RaResearchEducationActivityRow(
    Guid Id,
    string ActivityKindCode,
    string Title,
    string ExternalReference,
    DateTime RecordedAtUtc);
