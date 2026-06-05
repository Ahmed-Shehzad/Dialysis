using System.Text;
using Dialysis.HIE.Consent.Domain;
using Dialysis.HIE.Core.Abstraction.Consent;
using Dialysis.HIE.Outbound.CareSummary;
using Dialysis.HIE.Outbound.Domain;
using Dialysis.HIE.Persistence;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Tests.Outbound;

public sealed class CareSummaryAssemblerTests
{
    private const string Partner = "default";

    [Fact]
    public async Task Assembles_Ccd_From_Mapped_Resources_And_Queues_It_Async()
    {
        await using var factory = new HieWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var patientId = Guid.NewGuid();
        var db = sp.GetRequiredService<HieDbContext>();

        SeedConsent(db, patientId, purpose: null);
        SeedOutboundResource(db, patientId, new Patient
        {
            Id = patientId.ToString(),
            Name = [new HumanName { Family = "Doe", Given = ["Jane"] }],
            Gender = AdministrativeGender.Female,
        });
        SeedOutboundResource(db, patientId, new Observation
        {
            Id = "obs-1",
            Status = ObservationStatus.Final,
            Code = new CodeableConcept("http://loinc.org", "718-7", "Hemoglobin"),
            Subject = new ResourceReference($"Patient/{patientId}"),
            Value = new Quantity(11.2m, "g/dL"),
        });
        await db.SaveChangesAsync();

        var assembler = sp.GetRequiredService<CareSummaryAssembler>();
        var result = await assembler.AssembleAndEnqueueAsync(patientId, purposeOfUse: "Treatment");

        result.Generated.ShouldBeTrue();
        result.ResourceCount.ShouldBe(2);
        result.OutboundBundleId.ShouldNotBeNull();

        // A DocumentReference bundle was queued and its attachment decodes to a CCD.
        var queued = db.OutboundBundles.Single(b => b.Id == result.OutboundBundleId);
        queued.ResourceType.ShouldBe(nameof(DocumentReference));
        queued.Status.ShouldBe(OutboundBundleStatus.Pending);

        var parser = new FhirJsonDeserializer();
        var docRef = parser.Deserialize<DocumentReference>(queued.FhirJson);
        docRef.Type!.Coding.ShouldContain(c => c.Code == CareSummaryAssembler.CcdLoinc);
        var attachment = docRef.Content.ShouldHaveSingleItem().Attachment;
        attachment.ContentType.ShouldBe(CareSummaryAssembler.CcdContentType);
        attachment.Data.ShouldNotBeNull();

        var ccdXml = Encoding.UTF8.GetString(attachment.Data);
        ccdXml.ShouldContain("ClinicalDocument");
        ccdXml.ShouldContain("Continuity of Care Document");
        ccdXml.ShouldContain("Doe"); // recordTarget from the Patient resource
        ccdXml.ShouldContain("30954-2"); // Results section LOINC — the Observation landed in Results
    }

    [Fact]
    public async Task Suppresses_When_No_Consent_Async()
    {
        await using var factory = new HieWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var patientId = Guid.NewGuid();
        var db = sp.GetRequiredService<HieDbContext>();

        // No consent seeded.
        SeedOutboundResource(db, patientId, new Patient { Id = patientId.ToString() });
        await db.SaveChangesAsync();

        var assembler = sp.GetRequiredService<CareSummaryAssembler>();
        var result = await assembler.AssembleAndEnqueueAsync(patientId);

        result.Generated.ShouldBeFalse();
        db.OutboundBundles.Any(b => b.ResourceType == nameof(DocumentReference)).ShouldBeFalse();
    }

    [Fact]
    public async Task Returns_Not_Generated_When_No_Resources_Async()
    {
        await using var factory = new HieWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var patientId = Guid.NewGuid();
        var db = sp.GetRequiredService<HieDbContext>();
        SeedConsent(db, patientId, purpose: null);
        await db.SaveChangesAsync();

        var assembler = sp.GetRequiredService<CareSummaryAssembler>();
        var result = await assembler.AssembleAndEnqueueAsync(patientId);

        result.Generated.ShouldBeFalse();
        result.ResourceCount.ShouldBe(0);
    }

    private static void SeedConsent(HieDbContext db, Guid patientId, string? purpose) =>
        db.Consents.Add(new ConsentRecord(
            patientId, Partner, ConsentScopes.ClinicalNotes, ConsentDirection.Outbound,
            DateTime.UtcNow.AddMinutes(-1), effectiveToUtc: null, purpose: purpose));

    private static void SeedOutboundResource(HieDbContext db, Guid patientId, Resource resource) =>
        db.OutboundBundles.Add(new OutboundBundle(
            patientId,
            resource.TypeName,
            resource.Id ?? Guid.NewGuid().ToString(),
            Partner,
            resource.ToJson(),
            DateTime.UtcNow));
}
