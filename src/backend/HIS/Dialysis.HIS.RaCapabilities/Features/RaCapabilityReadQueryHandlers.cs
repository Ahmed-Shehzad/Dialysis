using Dialysis.CQRS.Queries;
using Dialysis.HIS.RaCapabilities.Features.ListResearchEducationActivities;
using Dialysis.HIS.RaCapabilities.Features.ListSpecialistEncounters;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features;

public sealed class ListOrganizationalCommunicationsQueryHandler(IRaCapabilitiesReadStore store)
    : IQueryHandler<ListOrganizationalCommunicationsQuery, IReadOnlyList<RaOrgCommunicationRow>>
{
    public Task<IReadOnlyList<RaOrgCommunicationRow>> HandleAsync(ListOrganizationalCommunicationsQuery request, CancellationToken cancellationToken) =>
        store.ListOrganizationalCommunicationsAsync(cancellationToken);
}

public sealed class ListQualityWorkflowTasksQueryHandler(IRaCapabilitiesReadStore store)
    : IQueryHandler<ListQualityWorkflowTasksQuery, IReadOnlyList<RaQualityWorkflowTaskRow>>
{
    public Task<IReadOnlyList<RaQualityWorkflowTaskRow>> HandleAsync(ListQualityWorkflowTasksQuery request, CancellationToken cancellationToken) =>
        store.ListQualityWorkflowTasksAsync(cancellationToken);
}

public sealed class ListFinancialErpLinksQueryHandler(IRaCapabilitiesReadStore store)
    : IQueryHandler<ListFinancialErpLinksQuery, IReadOnlyList<RaFinancialErpLinkRow>>
{
    public Task<IReadOnlyList<RaFinancialErpLinkRow>> HandleAsync(ListFinancialErpLinksQuery request, CancellationToken cancellationToken) =>
        store.ListFinancialErpLinksAsync(cancellationToken);
}

public sealed class ListWaitlistEntriesQueryHandler(IRaCapabilitiesReadStore store)
    : IQueryHandler<ListWaitlistEntriesQuery, IReadOnlyList<RaWaitlistEntryRow>>
{
    public Task<IReadOnlyList<RaWaitlistEntryRow>> HandleAsync(ListWaitlistEntriesQuery request, CancellationToken cancellationToken) =>
        store.ListWaitlistEntriesAsync(cancellationToken);
}

public sealed class ListEhrDocumentExchangesQueryHandler(IRaCapabilitiesReadStore store)
    : IQueryHandler<ListEhrDocumentExchangesQuery, IReadOnlyList<RaEhrDocumentExchangeRow>>
{
    public Task<IReadOnlyList<RaEhrDocumentExchangeRow>> HandleAsync(ListEhrDocumentExchangesQuery request, CancellationToken cancellationToken) =>
        store.ListEhrDocumentExchangesAsync(cancellationToken);
}

public sealed class ListPatientAlertsQueryHandler(IRaCapabilitiesReadStore store)
    : IQueryHandler<ListPatientAlertsQuery, IReadOnlyList<RaPatientAlertRow>>
{
    public Task<IReadOnlyList<RaPatientAlertRow>> HandleAsync(ListPatientAlertsQuery request, CancellationToken cancellationToken) =>
        store.ListPatientAlertsAsync(cancellationToken);
}

public sealed class ListMedicationDispensingRecordsQueryHandler(IRaCapabilitiesReadStore store)
    : IQueryHandler<ListMedicationDispensingRecordsQuery, IReadOnlyList<RaMedicationDispensingRow>>
{
    public Task<IReadOnlyList<RaMedicationDispensingRow>> HandleAsync(ListMedicationDispensingRecordsQuery request, CancellationToken cancellationToken) =>
        store.ListMedicationDispensingRecordsAsync(cancellationToken);
}

public sealed class ListClinicalDecisionSupportEvaluationsQueryHandler(IRaCapabilitiesReadStore store)
    : IQueryHandler<ListClinicalDecisionSupportEvaluationsQuery, IReadOnlyList<RaClinicalDecisionSupportRow>>
{
    public Task<IReadOnlyList<RaClinicalDecisionSupportRow>> HandleAsync(ListClinicalDecisionSupportEvaluationsQuery request, CancellationToken cancellationToken) =>
        store.ListClinicalDecisionSupportEvaluationsAsync(cancellationToken);
}

public sealed class ListAnalyticsExportJobsQueryHandler(IRaCapabilitiesReadStore store)
    : IQueryHandler<ListAnalyticsExportJobsQuery, IReadOnlyList<RaAnalyticsExportJobRow>>
{
    public Task<IReadOnlyList<RaAnalyticsExportJobRow>> HandleAsync(ListAnalyticsExportJobsQuery request, CancellationToken cancellationToken) =>
        store.ListAnalyticsExportJobsAsync(cancellationToken);
}

public sealed class ListFullTextSearchEntriesQueryHandler(IRaCapabilitiesReadStore store)
    : IQueryHandler<ListFullTextSearchEntriesQuery, IReadOnlyList<RaFullTextSearchEntryRow>>
{
    public Task<IReadOnlyList<RaFullTextSearchEntryRow>> HandleAsync(ListFullTextSearchEntriesQuery request, CancellationToken cancellationToken) =>
        store.ListFullTextSearchEntriesAsync(request.SearchTextContains, cancellationToken);
}

public sealed class ListSecurityMechanismHardeningsQueryHandler(IRaCapabilitiesReadStore store)
    : IQueryHandler<ListSecurityMechanismHardeningsQuery, IReadOnlyList<RaSecurityMechanismRow>>
{
    public Task<IReadOnlyList<RaSecurityMechanismRow>> HandleAsync(ListSecurityMechanismHardeningsQuery request, CancellationToken cancellationToken) =>
        store.ListSecurityMechanismHardeningsAsync(cancellationToken);
}

public sealed class ListSpecialistEncountersQueryHandler(IRaCapabilitiesReadStore store)
    : IQueryHandler<ListSpecialistEncountersQuery, IReadOnlyList<RaSpecialistEncounterRow>>
{
    public Task<IReadOnlyList<RaSpecialistEncounterRow>> HandleAsync(
        ListSpecialistEncountersQuery request,
        CancellationToken cancellationToken) =>
        store.ListSpecialistEncountersAsync(cancellationToken);
}

public sealed class ListResearchEducationActivitiesQueryHandler(IRaCapabilitiesReadStore store)
    : IQueryHandler<ListResearchEducationActivitiesQuery, IReadOnlyList<RaResearchEducationActivityRow>>
{
    public Task<IReadOnlyList<RaResearchEducationActivityRow>> HandleAsync(
        ListResearchEducationActivitiesQuery request,
        CancellationToken cancellationToken) =>
        store.ListResearchEducationActivitiesAsync(cancellationToken);
}
