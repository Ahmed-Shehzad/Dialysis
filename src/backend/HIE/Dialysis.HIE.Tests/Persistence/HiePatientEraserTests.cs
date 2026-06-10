using Dialysis.BuildingBlocks.DataProtection.DataSubjectRights;
using Dialysis.BuildingBlocks.DataProtection.Erasure;
using Dialysis.HIE.Consent.Domain;
using Dialysis.HIE.Outbound.Domain;
using Dialysis.HIE.Persistence;
using Dialysis.HIE.Persistence.Erasure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
// `Composition` alone would resolve to the Dialysis.HIE.Composition namespace.
using OpenEhrComposition = Dialysis.HIE.OpenEhr.Domain.Composition;

namespace Dialysis.HIE.Tests.Persistence;

/// <summary>
/// End-to-end smoke for the module-wide HIE contribution to the GDPR Art. 15/17/20 pipeline.
/// The composed <see cref="HiePatientEraser"/> must cover the patient-keyed tables beyond the
/// Documents slice — consents (<c>hie_consent</c>), outbound bundles (<c>hie_outbound</c>), and
/// openEHR compositions (<c>hie_openehr</c>) — via the real <c>ExecuteDeleteAsync</c> path on
/// Postgres, and the extractor must surface the same rows for export before they are erased.
/// </summary>
[Collection(nameof(HiePostgresFixtureCollection))]
public sealed class HiePatientEraserTests
{
    private readonly HiePostgresApiWebApplicationFactory _factory;

    /// <summary>
    /// End-to-end smoke for the module-wide HIE contribution to the GDPR Art. 15/17/20 pipeline.
    /// The composed <see cref="HiePatientEraser"/> must cover the patient-keyed tables beyond the
    /// Documents slice — consents (<c>hie_consent</c>), outbound bundles (<c>hie_outbound</c>), and
    /// openEHR compositions (<c>hie_openehr</c>) — via the real <c>ExecuteDeleteAsync</c> path on
    /// Postgres, and the extractor must surface the same rows for export before they are erased.
    /// </summary>
    public HiePatientEraserTests(HiePostgresApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Erase_Async_Hard_Deletes_Consents_Bundles_And_Compositions_For_The_Patient_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HieDbContext>();
        var patientId = Guid.CreateVersion7();
        var otherPatient = Guid.CreateVersion7();
        var now = DateTime.UtcNow;

        db.Consents.Add(new ConsentRecord(patientId, "partner-1", "patient-record", ConsentDirection.Bidirectional, now.AddDays(-1), null));
        db.OutboundBundles.Add(new OutboundBundle(patientId, "Patient", "p-1", "partner-1", """{"resourceType":"Patient"}""", now));
        db.OutboundBundles.Add(new OutboundBundle(patientId, "Encounter", "e-1", "partner-1", """{"resourceType":"Encounter"}""", now));
        db.Compositions.Add(new OpenEhrComposition(patientId, "openEHR-EHR-OBSERVATION.blood_pressure.v2", 1, "dr.house", now, "{}"));
        // Bystander rows the eraser must leave alone.
        db.Consents.Add(new ConsentRecord(otherPatient, "partner-1", "patient-record", ConsentDirection.Outbound, now.AddDays(-1), null));
        db.OutboundBundles.Add(new OutboundBundle(otherPatient, "Patient", "p-2", "partner-1", """{"resourceType":"Patient"}""", now));
        await db.SaveChangesAsync(CancellationToken.None);

        // Resolve through DI so the test also pins the composition: the module-wide eraser is
        // the single registered IPatientEraser, keying one coherent "hie" audit entry.
        var sut = scope.ServiceProvider.GetRequiredService<IPatientEraser>();
        sut.ShouldBeOfType<HiePatientEraser>();
        sut.ModuleSlug.ShouldBe("hie");

        var result = await sut.EraseAsync(patientId, "dpo@dialysis.test", CancellationToken.None);

        result.RecordsErased.ShouldBe(4);
        result.ByCategory["ConsentRecord"].ShouldBe(1);
        result.ByCategory["OutboundBundle"].ShouldBe(2);
        result.ByCategory["OpenEhrComposition"].ShouldBe(1);

        (await db.Consents.AsNoTracking().CountAsync(c => c.PatientId == patientId, CancellationToken.None)).ShouldBe(0);
        (await db.OutboundBundles.AsNoTracking().CountAsync(b => b.PatientId == patientId, CancellationToken.None)).ShouldBe(0);
        (await db.Compositions.AsNoTracking().CountAsync(c => c.PatientId == patientId, CancellationToken.None)).ShouldBe(0);

        // Bystander rows survive.
        (await db.Consents.AsNoTracking().CountAsync(c => c.PatientId == otherPatient, CancellationToken.None)).ShouldBe(1);
        (await db.OutboundBundles.AsNoTracking().CountAsync(b => b.PatientId == otherPatient, CancellationToken.None)).ShouldBe(1);

        // Idempotent on the second pass.
        var second = await sut.EraseAsync(patientId, "dpo@dialysis.test", CancellationToken.None);
        second.RecordsErased.ShouldBe(0);
    }

    [Fact]
    public async Task Extract_Async_Surfaces_The_Same_Patient_Keyed_Rows_The_Eraser_Covers_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HieDbContext>();
        var patientId = Guid.CreateVersion7();
        var now = DateTime.UtcNow;

        db.Consents.Add(new ConsentRecord(patientId, "partner-1", "patient-record", ConsentDirection.Inbound, now.AddDays(-1), null));
        db.OutboundBundles.Add(new OutboundBundle(patientId, "Patient", "p-9", "partner-1", """{"resourceType":"Patient","id":"p-9"}""", now));
        db.Compositions.Add(new OpenEhrComposition(patientId, "openEHR-EHR-OBSERVATION.lab_test_result.v1", 1, "dr.house", now, "{}"));
        await db.SaveChangesAsync(CancellationToken.None);

        var sut = scope.ServiceProvider.GetRequiredService<IModuleDataExtractor>();
        sut.ModuleSlug.ShouldBe("hie");

        var resources = await sut.ExtractAsync(patientId, CancellationToken.None);

        resources.Count.ShouldBe(3);
        resources.Single(r => r.ResourceType == "Consent").Json.ShouldContain("partner-1");
        // Outbound bundles surface their stored FHIR JSON verbatim.
        resources.Single(r => r.ResourceType == "Patient").Json.ShouldContain("\"id\":\"p-9\"");
        resources.Single(r => r.ResourceType == "OpenEhrComposition").Json.ShouldContain("lab_test_result");
    }
}
