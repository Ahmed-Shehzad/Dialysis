using System.Globalization;

using BuildingBlocks.ValueObjects;

using Dialysis.Patient.Application.Abstractions;
using Dialysis.Patient.Application.Domain.ValueObjects;

using Intercessor.Abstractions;

using DomainPatient = Dialysis.Patient.Application.Domain.Patient;

namespace Dialysis.Patient.Application.Features.IngestRspK22;

internal sealed class IngestRspK22CommandHandler : ICommandHandler<IngestRspK22Command, IngestRspK22Response>
{
    private const string StatusNf = "NF";
    private const string StatusAe = "AE";
    private const string StatusAr = "AR";

    private readonly IRspK22PatientParser _parser;
    private readonly IPatientRepository _repository;

    public IngestRspK22CommandHandler(IRspK22PatientParser parser, IPatientRepository repository)
    {
        _parser = parser;
        _repository = repository;
    }

    public async Task<IngestRspK22Response> HandleAsync(IngestRspK22Command request, CancellationToken cancellationToken = default)
    {
        RspK22PatientParseResult result = _parser.Parse(request.RawHl7Message);

        bool skipIngest = string.Equals(result.QakStatus, StatusNf, StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(result.QakStatus, StatusAe, StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(result.QakStatus, StatusAr, StringComparison.OrdinalIgnoreCase);

        if (skipIngest)
            return new IngestRspK22Response(0, result.QakStatus, Skipped: true);

        int ingestedCount = 0;

        foreach (PidPatientData data in result.Patients)
        {
            if (string.IsNullOrWhiteSpace(data.Identifier))
                continue;

            DomainPatient? existing = await FindExistingPatientAsync(data, cancellationToken);
            Person name = new(data.FirstName ?? "Unknown", data.LastName ?? "Unknown");
            DateOnly? dob = ParseDateOfBirth(data.DateOfBirth);
            Gender? gender = ParseGender(data.Gender);

            if (existing is not null)
            {
                string? pn = data.IdentifierType?.Equals("PN", StringComparison.OrdinalIgnoreCase) == true ? data.Identifier : null;
                string? ss = data.IdentifierType?.Equals("SS", StringComparison.OrdinalIgnoreCase) == true ? data.Identifier : null;
                existing.UpdateDemographics(pn, ss, name, dob, gender);
                _repository.Update(existing);
                ingestedCount++;
            }
            else
            {
                string? type = data.IdentifierType?.Trim().ToUpperInvariant();
                string mrnValue;
                string? personNumber = null;
                string? ssn = null;

                if (type is "MR" or "U" or null)
                    mrnValue = data.Identifier!;
                else if (type == "PN")
                {
                    mrnValue = Ulid.NewUlid().ToString();
                    personNumber = data.Identifier;
                }
                else if (type == "SS")
                {
                    mrnValue = Ulid.NewUlid().ToString();
                    ssn = data.Identifier;
                }
                else
                    mrnValue = data.Identifier!;

                var patient = DomainPatient.Register(
                    new MedicalRecordNumber(mrnValue),
                    name,
                    dob,
                    gender,
                    personNumber: personNumber,
                    socialSecurityNumber: ssn);
                await _repository.AddAsync(patient, cancellationToken);
                ingestedCount++;
            }
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return new IngestRspK22Response(ingestedCount, result.QakStatus, Skipped: false);
    }

    private async Task<DomainPatient?> FindExistingPatientAsync(PidPatientData data, CancellationToken ct)
    {
        string? type = data.IdentifierType?.Trim().ToUpperInvariant();

        if (type is "MR" or "U" or null)
            return await _repository.GetByMrnAsync(new MedicalRecordNumber(data.Identifier!), ct);

        if (type == "PN")
            return await _repository.GetByPersonNumberAsync(data.Identifier!, ct);

        if (type == "SS")
            return await _repository.GetBySsnAsync(data.Identifier!, ct);

        return await _repository.GetByMrnAsync(new MedicalRecordNumber(data.Identifier!), ct);
    }

    private static DateOnly? ParseDateOfBirth(string? dob)
    {
        if (string.IsNullOrWhiteSpace(dob)) return null;
        return DateOnly.TryParseExact(dob, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly d) ? d : null;
    }

    private static Gender? ParseGender(string? g)
    {
        if (string.IsNullOrWhiteSpace(g)) return null;
        string v = g.Trim().ToUpperInvariant();
        return v switch
        {
            "M" => Gender.Male,
            "F" => Gender.Female,
            "O" => Gender.Other,
            "U" => Gender.Unknown,
            _ => null
        };
    }
}
