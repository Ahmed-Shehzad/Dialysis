using System.Runtime.CompilerServices;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Ports;
using Hl7.Fhir.Model;
using FhirContactPoint = Hl7.Fhir.Model.ContactPoint;
using FhirHumanName = Hl7.Fhir.Model.HumanName;
using FhirPatient = Hl7.Fhir.Model.Patient;
using Patient = Dialysis.EHR.Registration.Domain.Patient;

namespace Dialysis.EHR.Registration.Fhir;

/// <summary>
/// Streams every EHR <see cref="Domain.Patient"/> aggregate as a FHIR R4 <c>Patient</c> resource for
/// inclusion in a Bulk Data <c>$export</c>. EHR is the patient-identity system of record;
/// the projection emits MRN as a business identifier, demographics, contact points, and
/// primary address.
/// </summary>
public sealed class EhrPatientFhirFeeder : INdjsonResourceFeeder<FhirPatient>
{
    private readonly IPatientRepository _patients;
    /// <summary>
    /// Streams every EHR <see cref="Domain.Patient"/> aggregate as a FHIR R4 <c>Patient</c> resource for
    /// inclusion in a Bulk Data <c>$export</c>. EHR is the patient-identity system of record;
    /// the projection emits MRN as a business identifier, demographics, contact points, and
    /// primary address.
    /// </summary>
    public EhrPatientFhirFeeder(IPatientRepository patients) => _patients = patients;
    public IAsyncEnumerable<FhirPatient> StreamAsync(ExportJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        return StreamCoreAsync(job, cancellationToken);
    }

    private async IAsyncEnumerable<FhirPatient> StreamCoreAsync(
        ExportJob job,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var patient in _patients.StreamAllAsync(job.Since, cancellationToken).ConfigureAwait(false))
        {
            yield return Project(patient);
        }
    }

    private static FhirPatient Project(Patient source)
    {
        var fhir = new FhirPatient
        {
            Id = source.Id.ToString(),
            Meta = new Meta { LastUpdated = source.UpdatedAtUtc },
            Identifier =
            [
                new Identifier
                {
                    System = "urn:dialysis:mrn",
                    Value = source.MedicalRecordNumber,
                    Use = Identifier.IdentifierUse.Usual,
                },
            ],
            Active = source.Status == PatientStatus.Active,
            Name =
            [
                new FhirHumanName
                {
                    Family = source.Name.FamilyName,
                    Given = string.IsNullOrEmpty(source.Name.MiddleName)
                        ? [source.Name.GivenName]
                        : [source.Name.GivenName, source.Name.MiddleName!],
                    Prefix = string.IsNullOrEmpty(source.Name.PrefixName) ? [] : [source.Name.PrefixName!],
                    Suffix = string.IsNullOrEmpty(source.Name.SuffixName) ? [] : [source.Name.SuffixName!],
                },
            ],
            BirthDate = source.DateOfBirth.ToString("yyyy-MM-dd"),
            Gender = MapGender(source.SexAtBirthCode),
            Deceased = source.Status == PatientStatus.Deceased ? new FhirBoolean(true) : null,
        };

        foreach (var contact in source.ContactPoints)
        {
            fhir.Telecom.Add(new FhirContactPoint
            {
                System = MapTelecomSystem(contact.System),
                Value = contact.Value,
                Use = MapTelecomUse(contact.Use),
            });
        }

        if (source.PrimaryAddress is { } addr)
        {
            fhir.Address.Add(new Address
            {
                Use = Address.AddressUse.Home,
                Line = string.IsNullOrEmpty(addr.Line2)
                    ? [addr.Line1]
                    : [addr.Line1, addr.Line2!],
                City = addr.City,
                State = addr.StateOrProvince,
                PostalCode = addr.PostalCode,
                Country = addr.CountryCode,
            });
        }

        if (!string.IsNullOrWhiteSpace(source.PreferredLanguageCode))
        {
            fhir.Communication =
            [
                new FhirPatient.CommunicationComponent
                {
                    Language = new CodeableConcept("urn:ietf:bcp:47", source.PreferredLanguageCode),
                    Preferred = true,
                },
            ];
        }

        return fhir;
    }

    private static AdministrativeGender? MapGender(string? sexAtBirthCode) =>
        sexAtBirthCode switch
        {
            "M" => AdministrativeGender.Male,
            "F" => AdministrativeGender.Female,
            "O" => AdministrativeGender.Other,
            "U" => AdministrativeGender.Unknown,
            _ => null,
        };

    private static FhirContactPoint.ContactPointSystem? MapTelecomSystem(ContactSystem system) =>
        system switch
        {
            ContactSystem.Phone => FhirContactPoint.ContactPointSystem.Phone,
            ContactSystem.Email => FhirContactPoint.ContactPointSystem.Email,
            ContactSystem.Sms => FhirContactPoint.ContactPointSystem.Sms,
            _ => null,
        };

    private static FhirContactPoint.ContactPointUse? MapTelecomUse(ContactUse use) =>
        use switch
        {
            ContactUse.Home => FhirContactPoint.ContactPointUse.Home,
            ContactUse.Work => FhirContactPoint.ContactPointUse.Work,
            ContactUse.Mobile => FhirContactPoint.ContactPointUse.Mobile,
            _ => null,
        };
}
