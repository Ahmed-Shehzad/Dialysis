using Dialysis.PublicHealth.Models;
using Dialysis.PublicHealth.Services;
using Intercessor.Abstractions;

namespace Dialysis.PublicHealth.Features.ReportableConditions;

public sealed class ListReportableConditionsHandler : IQueryHandler<ListReportableConditionsQuery, IReadOnlyList<ReportableCondition>>
{
    private readonly IReportableConditionCatalog _catalog;

    public ListReportableConditionsHandler(IReportableConditionCatalog catalog)
    {
        _catalog = catalog;
    }

    public async Task<IReadOnlyList<ReportableCondition>> HandleAsync(ListReportableConditionsQuery request, CancellationToken cancellationToken = default) =>
        await _catalog.ListAsync(request.Jurisdiction, cancellationToken);
}
