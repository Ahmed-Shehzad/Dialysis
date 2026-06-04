using Dialysis.CQRS.Queries;
using Dialysis.HIS.RaCapabilities.Features.ListResearchEducationActivities;
using Dialysis.HIS.RaCapabilities.Features.ListSpecialistEncounters;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features;

public sealed class ListOrganizationalCommunicationsQueryHandler : IQueryHandler<ListOrganizationalCommunicationsQuery, IReadOnlyList<RaOrgCommunicationRow>>
{
    private readonly IRaCapabilitiesReadStore _store;
    public ListOrganizationalCommunicationsQueryHandler(IRaCapabilitiesReadStore store) => _store = store;
    public Task<IReadOnlyList<RaOrgCommunicationRow>> HandleAsync(ListOrganizationalCommunicationsQuery request, CancellationToken cancellationToken) =>
        _store.ListOrganizationalCommunicationsAsync(cancellationToken);
}

public sealed class ListQualityWorkflowTasksQueryHandler : IQueryHandler<ListQualityWorkflowTasksQuery, IReadOnlyList<RaQualityWorkflowTaskRow>>
{
    private readonly IRaCapabilitiesReadStore _store;
    public ListQualityWorkflowTasksQueryHandler(IRaCapabilitiesReadStore store) => _store = store;
    public Task<IReadOnlyList<RaQualityWorkflowTaskRow>> HandleAsync(ListQualityWorkflowTasksQuery request, CancellationToken cancellationToken) =>
        _store.ListQualityWorkflowTasksAsync(cancellationToken);
}

public sealed class ListFinancialErpLinksQueryHandler : IQueryHandler<ListFinancialErpLinksQuery, IReadOnlyList<RaFinancialErpLinkRow>>
{
    private readonly IRaCapabilitiesReadStore _store;
    public ListFinancialErpLinksQueryHandler(IRaCapabilitiesReadStore store) => _store = store;
    public Task<IReadOnlyList<RaFinancialErpLinkRow>> HandleAsync(ListFinancialErpLinksQuery request, CancellationToken cancellationToken) =>
        _store.ListFinancialErpLinksAsync(cancellationToken);
}

public sealed class ListWaitlistEntriesQueryHandler : IQueryHandler<ListWaitlistEntriesQuery, IReadOnlyList<RaWaitlistEntryRow>>
{
    private readonly IRaCapabilitiesReadStore _store;
    public ListWaitlistEntriesQueryHandler(IRaCapabilitiesReadStore store) => _store = store;
    public Task<IReadOnlyList<RaWaitlistEntryRow>> HandleAsync(ListWaitlistEntriesQuery request, CancellationToken cancellationToken) =>
        _store.ListWaitlistEntriesAsync(cancellationToken);
}

public sealed class ListEhrDocumentExchangesQueryHandler : IQueryHandler<ListEhrDocumentExchangesQuery, IReadOnlyList<RaEhrDocumentExchangeRow>>
{
    private readonly IRaCapabilitiesReadStore _store;
    public ListEhrDocumentExchangesQueryHandler(IRaCapabilitiesReadStore store) => _store = store;
    public Task<IReadOnlyList<RaEhrDocumentExchangeRow>> HandleAsync(ListEhrDocumentExchangesQuery request, CancellationToken cancellationToken) =>
        _store.ListEhrDocumentExchangesAsync(cancellationToken);
}

public sealed class ListPatientAlertsQueryHandler : IQueryHandler<ListPatientAlertsQuery, IReadOnlyList<RaPatientAlertRow>>
{
    private readonly IRaCapabilitiesReadStore _store;
    public ListPatientAlertsQueryHandler(IRaCapabilitiesReadStore store) => _store = store;
    public Task<IReadOnlyList<RaPatientAlertRow>> HandleAsync(ListPatientAlertsQuery request, CancellationToken cancellationToken) =>
        _store.ListPatientAlertsAsync(cancellationToken);
}

public sealed class ListMedicationDispensingRecordsQueryHandler : IQueryHandler<ListMedicationDispensingRecordsQuery, IReadOnlyList<RaMedicationDispensingRow>>
{
    private readonly IRaCapabilitiesReadStore _store;
    public ListMedicationDispensingRecordsQueryHandler(IRaCapabilitiesReadStore store) => _store = store;
    public Task<IReadOnlyList<RaMedicationDispensingRow>> HandleAsync(ListMedicationDispensingRecordsQuery request, CancellationToken cancellationToken) =>
        _store.ListMedicationDispensingRecordsAsync(cancellationToken);
}

public sealed class ListClinicalDecisionSupportEvaluationsQueryHandler : IQueryHandler<ListClinicalDecisionSupportEvaluationsQuery, IReadOnlyList<RaClinicalDecisionSupportRow>>
{
    private readonly IRaCapabilitiesReadStore _store;
    public ListClinicalDecisionSupportEvaluationsQueryHandler(IRaCapabilitiesReadStore store) => _store = store;
    public Task<IReadOnlyList<RaClinicalDecisionSupportRow>> HandleAsync(ListClinicalDecisionSupportEvaluationsQuery request, CancellationToken cancellationToken) =>
        _store.ListClinicalDecisionSupportEvaluationsAsync(cancellationToken);
}

public sealed class ListAnalyticsExportJobsQueryHandler : IQueryHandler<ListAnalyticsExportJobsQuery, IReadOnlyList<RaAnalyticsExportJobRow>>
{
    private readonly IRaCapabilitiesReadStore _store;
    public ListAnalyticsExportJobsQueryHandler(IRaCapabilitiesReadStore store) => _store = store;
    public Task<IReadOnlyList<RaAnalyticsExportJobRow>> HandleAsync(ListAnalyticsExportJobsQuery request, CancellationToken cancellationToken) =>
        _store.ListAnalyticsExportJobsAsync(cancellationToken);
}

public sealed class ListFullTextSearchEntriesQueryHandler : IQueryHandler<ListFullTextSearchEntriesQuery, IReadOnlyList<RaFullTextSearchEntryRow>>
{
    private readonly IRaCapabilitiesReadStore _store;
    public ListFullTextSearchEntriesQueryHandler(IRaCapabilitiesReadStore store) => _store = store;
    public Task<IReadOnlyList<RaFullTextSearchEntryRow>> HandleAsync(ListFullTextSearchEntriesQuery request, CancellationToken cancellationToken) =>
        _store.ListFullTextSearchEntriesAsync(request.SearchTextContains, cancellationToken);
}

public sealed class ListSecurityMechanismHardeningsQueryHandler : IQueryHandler<ListSecurityMechanismHardeningsQuery, IReadOnlyList<RaSecurityMechanismRow>>
{
    private readonly IRaCapabilitiesReadStore _store;
    public ListSecurityMechanismHardeningsQueryHandler(IRaCapabilitiesReadStore store) => _store = store;
    public Task<IReadOnlyList<RaSecurityMechanismRow>> HandleAsync(ListSecurityMechanismHardeningsQuery request, CancellationToken cancellationToken) =>
        _store.ListSecurityMechanismHardeningsAsync(cancellationToken);
}

public sealed class ListSpecialistEncountersQueryHandler : IQueryHandler<ListSpecialistEncountersQuery, IReadOnlyList<RaSpecialistEncounterRow>>
{
    private readonly IRaCapabilitiesReadStore _store;
    public ListSpecialistEncountersQueryHandler(IRaCapabilitiesReadStore store) => _store = store;
    public Task<IReadOnlyList<RaSpecialistEncounterRow>> HandleAsync(
        ListSpecialistEncountersQuery request,
        CancellationToken cancellationToken) =>
        _store.ListSpecialistEncountersAsync(cancellationToken);
}

public sealed class ListResearchEducationActivitiesQueryHandler : IQueryHandler<ListResearchEducationActivitiesQuery, IReadOnlyList<RaResearchEducationActivityRow>>
{
    private readonly IRaCapabilitiesReadStore _store;
    public ListResearchEducationActivitiesQueryHandler(IRaCapabilitiesReadStore store) => _store = store;
    public Task<IReadOnlyList<RaResearchEducationActivityRow>> HandleAsync(
        ListResearchEducationActivitiesQuery request,
        CancellationToken cancellationToken) =>
        _store.ListResearchEducationActivitiesAsync(cancellationToken);
}
