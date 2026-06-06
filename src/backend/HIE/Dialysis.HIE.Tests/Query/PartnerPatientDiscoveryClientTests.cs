using Dialysis.HIE.Inbound.Mpi;
using Dialysis.HIE.Query;
using Hl7.Fhir.Model;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Tests.Query;

public sealed class PartnerPatientDiscoveryClientTests
{
    [Fact]
    public async Task Discovers_Partner_Patient_Ids_From_Demographics_Async()
    {
        var query = new RecordingQuery(
        [
            new Patient { Id = "ext-1", Name = [new HumanName { Family = "Doe", Given = ["Jane"] }] },
            new Patient { Id = "ext-2", Name = [new HumanName { Family = "Doe", Given = ["Janet"] }] },
        ]);
        var discovery = new PartnerPatientDiscoveryClient(query);

        var results = await discovery.DiscoverAsync(
            Guid.NewGuid(),
            new PatientMatchCriteria(Mrn: "MRN1", FamilyName: "Doe", GivenName: "Jane", DateOfBirth: new DateOnly(1980, 1, 15), SexAtBirthCode: null),
            subject: "local-1",
            purposeOfUse: "Treatment");

        results.Select(r => r.PartnerPatientId).ShouldBe(["ext-1", "ext-2"]);
        results[0].DisplayName.ShouldBe("Doe, Jane");
        // The search query carried the demographic parameters.
        query.LastQuery.ShouldNotBeNull();
        query.LastQuery!.ShouldContain("family=Doe");
        query.LastQuery.ShouldContain("given=Jane");
        query.LastQuery.ShouldContain("birthdate=1980-01-15");
        query.LastQuery.ShouldContain("identifier=MRN1");
    }

    [Fact]
    public async Task Refuses_A_Criteria_Free_Discovery_Async()
    {
        var discovery = new PartnerPatientDiscoveryClient(new RecordingQuery([]));
        await Should.ThrowAsync<InvalidOperationException>(() => discovery.DiscoverAsync(
            Guid.NewGuid(),
            new PatientMatchCriteria(null, null, null, null, null),
            "local-1", "Treatment"));
    }

    private sealed class RecordingQuery : IPartnerFhirQuery
    {
        private readonly IReadOnlyList<Resource> _resources;
        public RecordingQuery(IReadOnlyList<Resource> resources) => _resources = resources;
        public string? LastQuery { get; private set; }

        public Task<IReadOnlyList<Resource>> QueryAsync(Guid partnerId, string relativeQuery, string subject, string purposeOfUse, CancellationToken cancellationToken = default)
        {
            LastQuery = relativeQuery;
            return Task.FromResult(_resources);
        }
    }
}
