using Dialysis.CQRS.Queries;
using Dialysis.HIS.DataServices.Ports;

namespace Dialysis.HIS.DataServices.Features.SearchPatients;

public sealed class SearchPatientsQueryHandler : IQueryHandler<SearchPatientsQuery, IReadOnlyList<PatientSearchRow>>
{
    private readonly IPatientSearchReadModel _readModel;
    public SearchPatientsQueryHandler(IPatientSearchReadModel readModel) => _readModel = readModel;
    public Task<IReadOnlyList<PatientSearchRow>> HandleAsync(SearchPatientsQuery request, CancellationToken cancellationToken) =>
        _readModel.SearchAsync(request.Q, request.Skip, request.Take, cancellationToken);
}
