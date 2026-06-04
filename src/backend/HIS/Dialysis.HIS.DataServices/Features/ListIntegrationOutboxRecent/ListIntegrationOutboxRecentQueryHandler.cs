using Dialysis.CQRS.Queries;
using Dialysis.HIS.DataServices.Ports;

namespace Dialysis.HIS.DataServices.Features.ListIntegrationOutboxRecent;

public sealed class ListIntegrationOutboxRecentQueryHandler : IQueryHandler<ListIntegrationOutboxRecentQuery, IReadOnlyList<IntegrationOutboxMetadataRow>>
{
    private readonly IIntegrationOutboxMetadataReadModel _readModel;
    public ListIntegrationOutboxRecentQueryHandler(IIntegrationOutboxMetadataReadModel readModel) => _readModel = readModel;
    public Task<IReadOnlyList<IntegrationOutboxMetadataRow>> HandleAsync(
        ListIntegrationOutboxRecentQuery request,
        CancellationToken cancellationToken) =>
        _readModel.ListRecentAsync(request.Take, cancellationToken);
}
