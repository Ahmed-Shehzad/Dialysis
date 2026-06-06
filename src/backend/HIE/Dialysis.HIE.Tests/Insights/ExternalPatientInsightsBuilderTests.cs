using Dialysis.HIE.Inbound.Domain;
using Dialysis.HIE.Inbound.Insights;
using Dialysis.HIE.Inbound.Ports;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Tests.Insights;

public sealed class ExternalPatientInsightsBuilderTests
{
    [Fact]
    public async Task Aggregates_Cross_Source_Records_For_The_Patient_Async()
    {
        var rows = new List<ReceivedResource>
        {
            Row("partner-a", new Patient { Id = "ext-1" }),
            Row("partner-a", Obs("ext-1", "718-7", "Hemoglobin")),
            Row("partner-b", Obs("ext-1", "718-7", "Hemoglobin")), // same LOINC, different source → duplicate
            Row("partner-b", new Encounter { Id = "e1", Subject = new ResourceReference("Patient/ext-1") }),
            Row("partner-a", new DocumentReference { Id = "d1", Status = DocumentReferenceStatus.Current, Subject = new ResourceReference("Patient/ext-1") }),
            Row("partner-c", Obs("ext-2", "2345-7", "Glucose")), // different patient → excluded
        };
        var builder = new ExternalPatientInsightsBuilder(new StubStore(rows));

        var summary = await builder.BuildAsync("ext-1", scan: 500, recentTake: 20);

        summary.Counts.Observations.ShouldBe(2);
        summary.Counts.Encounters.ShouldBe(1);
        summary.Counts.Documents.ShouldBe(1);
        summary.Counts.Total.ShouldBe(4); // excludes the Patient demographics + the ext-2 observation
        summary.SourceOrganizations.ShouldBe(["partner-a", "partner-b"]);
        summary.LastUpdatedUtc.ShouldNotBeNull();
        summary.Recent.Count.ShouldBe(4);

        var alert = summary.DuplicateTestAlerts.ShouldHaveSingleItem();
        alert.Code.ShouldBe("718-7");
        alert.SourceCount.ShouldBe(2);
        alert.Sources.ShouldBe(["partner-a", "partner-b"]);
    }

    [Fact]
    public async Task Empty_When_No_Records_Match_Async()
    {
        var builder = new ExternalPatientInsightsBuilder(new StubStore(
            [Row("partner-a", new Patient { Id = "someone-else" })]));

        var summary = await builder.BuildAsync("ext-1", 500, 20);

        summary.Counts.Total.ShouldBe(0);
        summary.SourceOrganizations.ShouldBeEmpty();
        summary.LastUpdatedUtc.ShouldBeNull();
        summary.DuplicateTestAlerts.ShouldBeEmpty();
    }

    [Fact]
    public async Task Surfaces_Medications_Allergies_Problems_And_Safety_Alerts_Async()
    {
        var rows = new List<ReceivedResource>
        {
            Row("partner-a", new Patient { Id = "ext-1" }),
            Row("partner-a", Med("ext-1", "7980", "Penicillin V")), // same med code from two sources
            Row("partner-b", Med("ext-1", "7980", "Penicillin V")),
            Row("partner-b", Allergy("ext-1", "Penicillin")), // matches the medication → conflict
            Row("partner-a", Problem("ext-1", "E11.9", "Type 2 diabetes")),
        };
        var builder = new ExternalPatientInsightsBuilder(new StubStore(rows));

        var summary = await builder.BuildAsync("ext-1", scan: 500, recentTake: 20);

        summary.Counts.Medications.ShouldBe(2);
        summary.Counts.Allergies.ShouldBe(1);
        summary.Counts.Problems.ShouldBe(1);
        summary.Counts.Other.ShouldBe(0);
        summary.Medications.Count.ShouldBe(2);
        summary.Allergies.ShouldHaveSingleItem().Display.ShouldBe("Penicillin");
        summary.Problems.ShouldHaveSingleItem().Display.ShouldBe("Type 2 diabetes");

        var dupMed = summary.DuplicateMedicationAlerts.ShouldHaveSingleItem();
        dupMed.Code.ShouldBe("7980");
        dupMed.Sources.ShouldBe(["partner-a", "partner-b"]);

        var conflict = summary.AllergyConflictAlerts.ShouldHaveSingleItem();
        conflict.AllergyDisplay.ShouldBe("Penicillin");
        conflict.MedicationDisplay.ShouldBe("Penicillin V");
    }

    private static Observation Obs(string patientId, string loinc, string display) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Status = ObservationStatus.Final,
        Code = new CodeableConcept("http://loinc.org", loinc, display),
        Subject = new ResourceReference($"Patient/{patientId}"),
        Effective = new FhirDateTime(2026, 6, 1),
    };

    private static MedicationStatement Med(string patientId, string rxnorm, string display) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Status = MedicationStatement.MedicationStatusCodes.Active,
        Medication = new CodeableConcept("http://www.nlm.nih.gov/research/umls/rxnorm", rxnorm, display),
        Subject = new ResourceReference($"Patient/{patientId}"),
    };

    private static AllergyIntolerance Allergy(string patientId, string display) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Code = new CodeableConcept { Text = display },
        Patient = new ResourceReference($"Patient/{patientId}"),
    };

    private static Condition Problem(string patientId, string icd10, string display) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Code = new CodeableConcept("http://hl7.org/fhir/sid/icd-10-cm", icd10, display),
        Subject = new ResourceReference($"Patient/{patientId}"),
    };

    private static ReceivedResource Row(string partnerId, Resource resource) => new(
        partnerId, resource.TypeName, resource.Id ?? Guid.NewGuid().ToString(),
        resource.ToJson(), DateTime.UtcNow, validationOutcome: "accepted");

    private sealed class StubStore : IReceivedResourceStore
    {
        private readonly IReadOnlyList<ReceivedResource> _rows;
        public StubStore(IReadOnlyList<ReceivedResource> rows) => _rows = rows;
        public Task UpsertAsync(ReceivedResource resource, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<ReceivedResource>> ListRecentAsync(string? partnerId, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult(_rows);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
