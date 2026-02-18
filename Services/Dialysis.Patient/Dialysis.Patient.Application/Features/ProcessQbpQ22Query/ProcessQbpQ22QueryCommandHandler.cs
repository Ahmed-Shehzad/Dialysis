using BuildingBlocks.ValueObjects;

using Dialysis.Patient.Application.Abstractions;
using Dialysis.Patient.Application.Domain.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Patient.Application.Features.ProcessQbpQ22Query;

internal sealed class ProcessQbpQ22QueryCommandHandler : ICommandHandler<ProcessQbpQ22QueryCommand, ProcessQbpQ22QueryResponse>
{
    private readonly IQbpQ22Parser _parser;
    private readonly IPatientRspK22Builder _builder;
    private readonly IPatientRepository _repository;

    public ProcessQbpQ22QueryCommandHandler(IQbpQ22Parser parser, IPatientRspK22Builder builder, IPatientRepository repository)
    {
        _parser = parser;
        _builder = builder;
        _repository = repository;
    }

    public async Task<ProcessQbpQ22QueryResponse> HandleAsync(ProcessQbpQ22QueryCommand request, CancellationToken cancellationToken = default)
    {
        QbpQ22ParseResult query = _parser.Parse(request.RawHl7Message);

        IReadOnlyList<Domain.Patient> patients;

        if (!string.IsNullOrWhiteSpace(query.Mrn))
        {
            Domain.Patient? patient = await _repository.GetByMrnAsync(
                new MedicalRecordNumber(query.Mrn), cancellationToken);
            patients = patient is not null ? [patient] : [];
        }
        else
        {
            patients = await _repository.SearchByNameAsync(
                new Person(query.FirstName ?? "", query.LastName ?? ""), cancellationToken);
        }

        string rspK22 = patients.Count > 0
            ? _builder.BuildFromPatients(patients, query)
            : _builder.BuildNoDataFound(query);

        return new ProcessQbpQ22QueryResponse(rspK22, patients.Count);
    }
}
