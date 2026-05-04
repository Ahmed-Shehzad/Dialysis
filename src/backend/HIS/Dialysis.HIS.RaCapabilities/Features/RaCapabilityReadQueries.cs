using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features;

public sealed record ListOrganizationalCommunicationsQuery
    : IQuery<IReadOnlyList<RaOrgCommunicationRow>>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCapabilitiesRead;
}

public sealed record ListQualityWorkflowTasksQuery
    : IQuery<IReadOnlyList<RaQualityWorkflowTaskRow>>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCapabilitiesRead;
}

public sealed record ListFinancialErpLinksQuery
    : IQuery<IReadOnlyList<RaFinancialErpLinkRow>>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCapabilitiesRead;
}

public sealed record ListWaitlistEntriesQuery
    : IQuery<IReadOnlyList<RaWaitlistEntryRow>>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCapabilitiesRead;
}

public sealed record ListEhrDocumentExchangesQuery
    : IQuery<IReadOnlyList<RaEhrDocumentExchangeRow>>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCapabilitiesRead;
}

public sealed record ListPatientAlertsQuery
    : IQuery<IReadOnlyList<RaPatientAlertRow>>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCapabilitiesRead;
}

public sealed record ListMedicationDispensingRecordsQuery
    : IQuery<IReadOnlyList<RaMedicationDispensingRow>>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCapabilitiesRead;
}

public sealed record ListClinicalDecisionSupportEvaluationsQuery
    : IQuery<IReadOnlyList<RaClinicalDecisionSupportRow>>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCapabilitiesRead;
}

public sealed record ListAnalyticsExportJobsQuery
    : IQuery<IReadOnlyList<RaAnalyticsExportJobRow>>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCapabilitiesRead;
}

public sealed record ListFullTextSearchEntriesQuery(string? SearchTextContains = null)
    : IQuery<IReadOnlyList<RaFullTextSearchEntryRow>>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCapabilitiesRead;
}

public sealed record ListSecurityMechanismHardeningsQuery
    : IQuery<IReadOnlyList<RaSecurityMechanismRow>>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCapabilitiesRead;
}
