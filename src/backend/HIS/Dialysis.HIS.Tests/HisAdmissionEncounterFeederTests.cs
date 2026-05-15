using System.Runtime.CompilerServices;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Domain.ValueObjects;
using Dialysis.HIS.PatientFlow.Fhir;
using Dialysis.HIS.PatientFlow.Ports;
using Hl7.Fhir.Model;
using Shouldly;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIS.Tests;

public sealed class HisAdmissionEncounterFeederTests
{
    [Fact]
    public async Task Streamasync_Projects_Each_Admission_To_Encounter_With_Subject_And_Status_Async()
    {
        var openAdmission = Admission.Admit(Guid.NewGuid(), new WardCode("WARD-A1"), DateTime.UtcNow);
        var dischargedAdmission = Admission.Admit(Guid.NewGuid(), new WardCode("WARD-B2"), DateTime.UtcNow.AddDays(-1));
        dischargedAdmission.Discharge(DateTime.UtcNow);

        var repo = new InMemoryAdmissions(openAdmission, dischargedAdmission);
        var feeder = new HisAdmissionEncounterFeeder(repo);
        var job = NewJob(since: null);

        var results = new List<Encounter>();
        await foreach (var encounter in feeder.StreamAsync(job, CancellationToken.None))
        {
            results.Add(encounter);
        }

        results.Count.ShouldBe(2);

        var openOut = results.First(e => e.Id == openAdmission.Id.ToString());
        openOut.Status.ShouldBe(Encounter.EncounterStatus.InProgress);
        openOut.Subject.Reference.ShouldBe($"Patient/{openAdmission.PatientId}");
        openOut.Period.End.ShouldBeNull();
        openOut.Location[0].Location.Reference.ShouldBe("Location/WARD-A1");

        var dischargedOut = results.First(e => e.Id == dischargedAdmission.Id.ToString());
        dischargedOut.Status.ShouldBe(Encounter.EncounterStatus.Finished);
        dischargedOut.Period.End.ShouldNotBeNull();
    }

    [Fact]
    public async Task Streamasync_Honours_Since_Filter_On_Latest_Activity_Timestamp_Async()
    {
        var keep = Admission.Admit(Guid.NewGuid(), new WardCode("WARD-A1"), DateTime.UtcNow);
        var drop = Admission.Admit(Guid.NewGuid(), new WardCode("WARD-A1"), DateTime.UtcNow.AddDays(-30));
        // drop was admitted and discharged a month ago — falls below the _since cutoff.
        drop.Discharge(DateTime.UtcNow.AddDays(-29));

        var feeder = new HisAdmissionEncounterFeeder(new InMemoryAdmissions(keep, drop));
        var job = NewJob(since: DateTimeOffset.UtcNow.AddDays(-7));

        var results = new List<Encounter>();
        await foreach (var encounter in feeder.StreamAsync(job, CancellationToken.None))
        {
            results.Add(encounter);
        }

        results.Count.ShouldBe(1);
        results[0].Id.ShouldBe(keep.Id.ToString());
    }

    private static ExportJob NewJob(DateTimeOffset? since) => new(
        Id: Guid.NewGuid().ToString("N"),
        Scope: ExportScope.System,
        GroupId: null,
        ResourceTypes: ["Encounter"],
        Since: since,
        DeIdentificationProfile: null,
        RequestorId: null,
        Status: ExportJobStatus.InProgress,
        CreatedAt: DateTimeOffset.UtcNow,
        CompletedAt: null,
        Outputs: Array.Empty<ExportJobOutput>(),
        Error: null);

    private sealed class InMemoryAdmissions(params Admission[] admissions) : IAdmissionRepository
    {
        public void Add(Admission admission) => throw new NotSupportedException();

        public Task<Admission?> GetAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(admissions.FirstOrDefault(a => a.Id == id));

        public async IAsyncEnumerable<Admission> StreamAllAsync(
            DateTimeOffset? since,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var admission in admissions)
            {
                if (since is { } cutoff)
                {
                    var latestUtc = admission.DischargedAtUtc ?? admission.AdmittedAtUtc;
                    if (latestUtc < cutoff.UtcDateTime) continue;
                }
                yield return admission;
                await Task.Yield();
            }
        }
    }
}
