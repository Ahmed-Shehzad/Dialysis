using System.Globalization;

using BuildingBlocks.Caching;
using BuildingBlocks.Tenancy;
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
    private const string PatientKeyPrefix = "patient";

    private readonly IRspK22PatientParser _parser;
    private readonly IPatientRepository _repository;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly ITenantContext _tenant;

    public IngestRspK22CommandHandler(IRspK22PatientParser parser, IPatientRepository repository, ICacheInvalidator cacheInvalidator, ITenantContext tenant)
    {
        _parser = parser;
        _repository = repository;
        _cacheInvalidator = cacheInvalidator;
        _tenant = tenant;
    }

    public async Task<IngestRspK22Response> HandleAsync(IngestRspK22Command request, CancellationToken cancellationToken = default)
    {
        RspK22PatientParseResult result = _parser.Parse(request.RawHl7Message);

        if (ShouldSkipIngest(result.QakStatus))
            return new IngestRspK22Response(0, result.QakStatus, Skipped: true);

        int ingestedCount = await IngestPatientsAsync(result.Patients, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return new IngestRspK22Response(ingestedCount, result.QakStatus, Skipped: false);
    }

    private static bool ShouldSkipIngest(string? qakStatus) =>
        string.Equals(qakStatus, StatusNf, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(qakStatus, StatusAe, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(qakStatus, StatusAr, StringComparison.OrdinalIgnoreCase);

    private async Task<int> IngestPatientsAsync(IReadOnlyList<PidPatientData> patients, CancellationToken ct)
    {
        int count = 0;
        foreach (PidPatientData data in patients)
        {
            if (string.IsNullOrWhiteSpace(data.Identifier))
                continue;
            await ProcessPatientAsync(data, ct);
            count++;
        }
        return count;
    }

    private async Task ProcessPatientAsync(PidPatientData data, CancellationToken ct)
    {
        DomainPatient? existing = await FindExistingPatientAsync(data, ct);
        Person name = new(data.FirstName ?? "Unknown", data.LastName ?? "Unknown");
        DateOnly? dob = ParseDateOfBirth(data.DateOfBirth);
        Gender? gender = ParseGender(data.Gender);

        if (existing is not null)
        {
            (string? pn, string? ss) = GetIdentifiersForUpdate(data);
            existing.UpdateDemographics(pn, ss, name, dob, gender);
            _repository.Update(existing);
            await InvalidatePatientCacheAsync(existing.MedicalRecordNumber.Value, existing.Id.ToString(), ct);
            return;
        }

        (string mrnValue, string? personNumber, string? ssn) = ResolveMrnAndIdentifiers(data);
        var patient = DomainPatient.Register(
            new MedicalRecordNumber(mrnValue),
            name,
            dob,
            gender,
            _tenant.TenantId,
            personNumber: personNumber,
            socialSecurityNumber: ssn);
        await _repository.AddAsync(patient, ct);
        await InvalidatePatientCacheAsync(patient.MedicalRecordNumber.Value, patient.Id.ToString(), ct);
    }

    private async Task InvalidatePatientCacheAsync(string mrn, string id, CancellationToken cancellationToken)
    {
        string[] keys = new[] { $"{_tenant.TenantId}:{PatientKeyPrefix}:{mrn}", $"{_tenant.TenantId}:{PatientKeyPrefix}:id:{id}" };
        await _cacheInvalidator.InvalidateAsync(keys, cancellationToken);
    }

    private static (string? pn, string? ss) GetIdentifiersForUpdate(PidPatientData data)
    {
        bool isPn = string.Equals(data.IdentifierType, "PN", StringComparison.OrdinalIgnoreCase);
        bool isSs = string.Equals(data.IdentifierType, "SS", StringComparison.OrdinalIgnoreCase);
        return (isPn ? data.Identifier : null, isSs ? data.Identifier : null);
    }

    private static (string mrnValue, string? personNumber, string? ssn) ResolveMrnAndIdentifiers(PidPatientData data)
    {
        string? type = data.IdentifierType?.Trim().ToUpperInvariant();
        return type switch
        {
            "PN" => (Ulid.NewUlid().ToString(), data.Identifier, null),
            "SS" => (Ulid.NewUlid().ToString(), null, data.Identifier),
            "MR" or "U" or null => (data.Identifier!, null, null),
            _ => (data.Identifier!, null, null)
        };
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
