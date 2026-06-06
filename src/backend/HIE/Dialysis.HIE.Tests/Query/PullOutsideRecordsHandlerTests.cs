using Dialysis.HIE.Inbound.Ingestion;
using Dialysis.HIE.Inbound.Mpi;
using Dialysis.HIE.Query;
using Dialysis.HIE.Query.Features.PullOutsideRecords;
using Dialysis.HIE.Query.Xca;
using Hl7.Fhir.Model;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Tests.Query;

public sealed class PullOutsideRecordsHandlerTests
{
    [Fact]
    public async Task Discovers_Then_Pulls_Records_And_Documents_Async()
    {
        await using var factory = new HieWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var ingestion = scope.ServiceProvider.GetRequiredService<InboundIngestionService>();

        var discovery = new FakeDiscovery([new DiscoveredPatient("ext-1", "Doe, Jane", null)]);
        var query = new FakeQuery([new Patient { Id = "ext-1" }, new Observation { Id = "o1", Status = ObservationStatus.Final }]);
        var xcaQuery = new FakeXcaQuery([new DocumentReference { Id = "d1", Status = DocumentReferenceStatus.Current }]);
        var xcaRetrieve = new FakeXcaRetrieve("ccd"u8.ToArray());
        var handler = new PullOutsideRecordsCommandHandler(discovery, query, xcaQuery, xcaRetrieve, ingestion);

        var result = await handler.HandleAsync(
            new PullOutsideRecordsCommand(Guid.NewGuid(), Mrn: "MRN1", Family: "Doe"), CancellationToken.None);

        result.Candidates.ShouldBe(1);
        result.ResolvedPartnerPatientId.ShouldBe("ext-1");
        result.RecordsFetched.ShouldBe(2);
        result.DocumentsFetched.ShouldBe(1);
        discovery.Called.ShouldBeTrue();
    }

    [Fact]
    public async Task Skips_Discovery_When_Partner_Patient_Id_Is_Supplied_Async()
    {
        await using var factory = new HieWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var ingestion = scope.ServiceProvider.GetRequiredService<InboundIngestionService>();

        var discovery = new FakeDiscovery([]);
        var handler = new PullOutsideRecordsCommandHandler(
            discovery, new FakeQuery([]), new FakeXcaQuery([]), new FakeXcaRetrieve(null), ingestion);

        var result = await handler.HandleAsync(
            new PullOutsideRecordsCommand(Guid.NewGuid(), PartnerPatientId: "ext-9"), CancellationToken.None);

        result.ResolvedPartnerPatientId.ShouldBe("ext-9");
        discovery.Called.ShouldBeFalse();
    }

    private sealed class FakeDiscovery(IReadOnlyList<DiscoveredPatient> result) : IPartnerPatientDiscovery
    {
        public bool Called { get; private set; }
        public Task<IReadOnlyList<DiscoveredPatient>> DiscoverAsync(Guid partnerId, PatientMatchCriteria criteria, string subject, string purposeOfUse, CancellationToken cancellationToken = default)
        {
            Called = true;
            return Task.FromResult(result);
        }
    }

    private sealed class FakeQuery(IReadOnlyList<Resource> result) : IPartnerFhirQuery
    {
        public Task<IReadOnlyList<Resource>> QueryAsync(Guid partnerId, string relativeQuery, string subject, string purposeOfUse, CancellationToken cancellationToken = default) =>
            Task.FromResult(result);
    }

    private sealed class FakeXcaQuery(IReadOnlyList<DocumentReference> result) : IXcaQueryClient
    {
        public Task<IReadOnlyList<DocumentReference>> QueryDocumentsAsync(Guid partnerId, string partnerPatientId, string purposeOfUse, CancellationToken cancellationToken = default) =>
            Task.FromResult(result);
    }

    private sealed class FakeXcaRetrieve(byte[]? content) : IXcaRetrieveClient
    {
        public Task<byte[]?> RetrieveContentAsync(Guid partnerId, DocumentReference document, string subject, string purposeOfUse, CancellationToken cancellationToken = default) =>
            Task.FromResult(content);
    }
}
