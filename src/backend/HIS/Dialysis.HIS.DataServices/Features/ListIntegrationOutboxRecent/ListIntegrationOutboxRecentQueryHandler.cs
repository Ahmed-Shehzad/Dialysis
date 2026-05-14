using Dialysis.CQRS.Queries;
using Dialysis.HIS.DataServices.Ports;

namespace Dialysis.HIS.DataServices.Features.ListIntegrationOutboxRecent;

public sealed class ListIntegrationOutboxRecentQueryHandler(IIntegrationOutboxMetadataReadModel readModel)
    : IQueryHandler<ListIntegrationOutboxRecentQuery, IReadOnlyList<IntegrationOutboxMetadataRow>>
{
    public Task<IReadOnlyList<IntegrationOutboxMetadataRow>> HandleAsync(
        ListIntegrationOutboxRecentQuery request,
        CancellationToken cancellationToken) =>
        readModel.ListRecentAsync(request.Take, cancellationToken);
}
