using Dialysis.CQRS;
using Dialysis.HIS.DataServices.Features.SearchPatients;
using Dialysis.HIS.DataServices.Ports;
using Dialysis.HIS.Persistence;
using Dialysis.HIS.Persistence.Repositories;
using Dialysis.HIS.RaCapabilities.Domain;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dialysis.HIS.Tests;

[Collection(nameof(HisFixtureCollection))]
public sealed class SearchPatientsFlowTests(HisApiWebApplicationFactory factory)
{
    [Fact]
    public async Task Returns_Only_Patients_Corpus_Entries_Matching_Q_Async()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();

        var now = DateTime.UtcNow;
        await db.RaFullTextSearchEntries.AddRangeAsync(
            new RaFullTextSearchEntry { Id = Guid.CreateVersion7(), CorpusCode = EfPatientSearchReadModel.PatientCorpusCode, ExternalId = "MRN-1", SearchText = "Alice Johnson", IndexedAtUtc = now },
            new RaFullTextSearchEntry { Id = Guid.CreateVersion7(), CorpusCode = EfPatientSearchReadModel.PatientCorpusCode, ExternalId = "MRN-2", SearchText = "Bob Smith", IndexedAtUtc = now },
            new RaFullTextSearchEntry { Id = Guid.CreateVersion7(), CorpusCode = "orders", ExternalId = "ORD-9", SearchText = "Aspirin order", IndexedAtUtc = now });
        await db.SaveChangesAsync(CancellationToken.None);

        var rows = await gateway.SendQueryAsync<SearchPatientsQuery, IReadOnlyList<PatientSearchRow>>(
            new SearchPatientsQuery(Q: "Alice"),
            CancellationToken.None);

        rows.Count.ShouldBe(1);
        rows[0].SearchText.ShouldContain("Alice");
        rows[0].ExternalPatientId.ShouldBe("MRN-1");
    }

    [Fact]
    public async Task Empty_Q_Returns_All_Patient_Rows_Clamped_To_Take_Async()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();

        for (var i = 0; i < 5; i++)
        {
            db.RaFullTextSearchEntries.Add(new RaFullTextSearchEntry
            {
                Id = Guid.CreateVersion7(),
                CorpusCode = EfPatientSearchReadModel.PatientCorpusCode,
                ExternalId = $"MRN-{i:D3}",
                SearchText = $"Patient {i}",
                IndexedAtUtc = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync(CancellationToken.None);

        var rows = await gateway.SendQueryAsync<SearchPatientsQuery, IReadOnlyList<PatientSearchRow>>(
            new SearchPatientsQuery(Take: 2),
            CancellationToken.None);

        rows.Count.ShouldBeLessThanOrEqualTo(2);
    }
}
