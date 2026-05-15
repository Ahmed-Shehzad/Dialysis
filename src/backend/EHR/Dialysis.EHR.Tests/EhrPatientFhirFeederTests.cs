using System.Runtime.CompilerServices;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Fhir;
using Dialysis.EHR.Registration.Ports;
using Shouldly;
using Xunit;
using FhirGender = Hl7.Fhir.Model.AdministrativeGender;
using FhirPatient = Hl7.Fhir.Model.Patient;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests;

public sealed class EhrPatientFhirFeederTests
{
    [Fact]
    public async Task Streamasync_Projects_Mrn_Name_Birthdate_And_Gender_Async()
    {
        var patient = Patient.Register(
            id: Guid.NewGuid(),
            medicalRecordNumber: "MRN-001",
            name: new HumanName("Doe", "Jane", middleName: "A"),
            dateOfBirth: new DateOnly(1980, 3, 17),
            sexAtBirthCode: "F",
            preferredLanguageCode: "en");

        var feeder = new EhrPatientFhirFeeder(new InMemoryPatients(patient));

        var results = new List<FhirPatient>();
        await foreach (var resource in feeder.StreamAsync(NewJob(), CancellationToken.None))
        {
            results.Add(resource);
        }

        results.Count.ShouldBe(1);
        var fhir = results[0];
        fhir.Id.ShouldBe(patient.Id.ToString());
        fhir.Identifier[0].System.ShouldBe("urn:dialysis:mrn");
        fhir.Identifier[0].Value.ShouldBe("MRN-001");
        fhir.Name[0].Family.ShouldBe("Doe");
        fhir.Name[0].Given.ToList().ShouldBe(["Jane", "A"]);
        fhir.BirthDate.ShouldBe("1980-03-17");
        fhir.Gender.ShouldBe(FhirGender.Female);
        fhir.Active.ShouldBe(true);
        fhir.Communication[0].Language.Coding[0].Code.ShouldBe("en");
    }

    private static ExportJob NewJob() => new(
        Id: Guid.NewGuid().ToString("N"),
        Scope: ExportScope.System,
        GroupId: null,
        ResourceTypes: ["Patient"],
        Since: null,
        DeIdentificationProfile: null,
        RequestorId: null,
        Status: ExportJobStatus.InProgress,
        CreatedAt: DateTimeOffset.UtcNow,
        CompletedAt: null,
        Outputs: Array.Empty<ExportJobOutput>(),
        Error: null);

    private sealed class InMemoryPatients(params Patient[] patients) : IPatientRepository
    {
        public void Add(Patient patient) => throw new NotSupportedException();

        public Task<Patient?> GetAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(patients.FirstOrDefault(p => p.Id == id));

        public Task<Patient?> FindByMedicalRecordNumberAsync(string medicalRecordNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(patients.FirstOrDefault(p => p.MedicalRecordNumber == medicalRecordNumber));

        public Task<IReadOnlyList<Patient>> SearchAsync(string? nameFragment, int take, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Patient>>(patients.Take(take).ToList());

        public Task<PatientSearchPage> SearchAsync(PatientSearchCriteria criteria, CancellationToken cancellationToken = default)
            => Task.FromResult(new PatientSearchPage(patients.ToList(), patients.Length));

        public async IAsyncEnumerable<Patient> StreamAllAsync(
            DateTimeOffset? since,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _ = since;
            foreach (var p in patients)
            {
                yield return p;
                await Task.Yield();
            }
        }
    }
}
