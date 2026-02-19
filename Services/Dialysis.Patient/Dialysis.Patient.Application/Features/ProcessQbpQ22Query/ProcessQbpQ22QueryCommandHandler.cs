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
        IReadOnlyList<Domain.Patient> patients = await ResolvePatientsAsync(query, cancellationToken);

        string rspK22 = patients.Count > 0
            ? _builder.BuildFromPatients(patients, query)
            : _builder.BuildNoDataFound(query);

        return new ProcessQbpQ22QueryResponse(rspK22, patients.Count);
    }

    private async Task<IReadOnlyList<Domain.Patient>> ResolvePatientsAsync(QbpQ22ParseResult query, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(query.Mrn))
            return await ResolveSingleAsync(_repository.GetByMrnAsync(new MedicalRecordNumber(query.Mrn), ct));

        if (!string.IsNullOrWhiteSpace(query.PersonNumber))
            return await ResolveSingleAsync(_repository.GetByPersonNumberAsync(query.PersonNumber, ct));

        if (!string.IsNullOrWhiteSpace(query.SocialSecurityNumber))
            return await ResolveSingleAsync(_repository.GetBySsnAsync(query.SocialSecurityNumber, ct));

        if (!string.IsNullOrWhiteSpace(query.UniversalId))
            return await ResolveSingleAsync(_repository.GetByMrnAsync(new MedicalRecordNumber(query.UniversalId), ct));

        return await ResolveByNameAsync(query, ct);
    }

    private static async Task<IReadOnlyList<Domain.Patient>> ResolveSingleAsync(Task<Domain.Patient?> lookup)
    {
        Domain.Patient? patient = await lookup;
        return patient is not null ? [patient] : [];
    }

    private async Task<IReadOnlyList<Domain.Patient>> ResolveByNameAsync(QbpQ22ParseResult query, CancellationToken ct)
    {
        string? firstName = query.FirstName;
        string? lastName = query.LastName;

        if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
            return await _repository.SearchByNameAsync(new Person(firstName, lastName), ct);

        if (!string.IsNullOrWhiteSpace(lastName))
            return await _repository.SearchByLastNameAsync(lastName, ct);

        return [];
    }
}
