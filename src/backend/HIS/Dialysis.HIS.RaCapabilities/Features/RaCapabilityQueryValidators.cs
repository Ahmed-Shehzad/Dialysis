using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.RaCapabilities.Features;

public sealed class ListOrganizationalCommunicationsQueryValidator : AbstractValidator<ListOrganizationalCommunicationsQuery>;

public sealed class ListQualityWorkflowTasksQueryValidator : AbstractValidator<ListQualityWorkflowTasksQuery>;

public sealed class ListFinancialErpLinksQueryValidator : AbstractValidator<ListFinancialErpLinksQuery>;

public sealed class ListWaitlistEntriesQueryValidator : AbstractValidator<ListWaitlistEntriesQuery>;

public sealed class ListEhrDocumentExchangesQueryValidator : AbstractValidator<ListEhrDocumentExchangesQuery>;

public sealed class ListPatientAlertsQueryValidator : AbstractValidator<ListPatientAlertsQuery>;

public sealed class ListMedicationDispensingRecordsQueryValidator : AbstractValidator<ListMedicationDispensingRecordsQuery>;

public sealed class ListClinicalDecisionSupportEvaluationsQueryValidator : AbstractValidator<ListClinicalDecisionSupportEvaluationsQuery>;

public sealed class ListAnalyticsExportJobsQueryValidator : AbstractValidator<ListAnalyticsExportJobsQuery>;

public sealed class ListFullTextSearchEntriesQueryValidator : AbstractValidator<ListFullTextSearchEntriesQuery>
{
    public ListFullTextSearchEntriesQueryValidator()
    {
        RuleFor(static c => c.SearchTextContains, nameof(ListFullTextSearchEntriesQuery.SearchTextContains))
            .Must(static (_, s) => s is null || s.Length <= 256)
            .WithMessage("Search text filter is too long.");
    }
}

public sealed class ListSecurityMechanismHardeningsQueryValidator : AbstractValidator<ListSecurityMechanismHardeningsQuery>;
