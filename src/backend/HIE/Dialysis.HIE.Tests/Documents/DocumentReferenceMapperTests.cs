using Dialysis.HIE.Documents.Domain;
using Dialysis.HIE.Documents.Fhir;
using Shouldly;
using Xunit;
using DocumentReferenceStatus = Hl7.Fhir.Model.DocumentReferenceStatus;

namespace Dialysis.HIE.Tests.Documents;

public sealed class DocumentReferenceMapperTests
{
    [Fact]
    public void To_Fhir_Maps_Core_Header_Fields()
    {
        var doc = new DocumentReference(
            id: Guid.Parse("00000000-0000-0000-0000-000000000001"),
            patientId: Guid.Parse("00000000-0000-0000-0000-0000000000A1"),
            kind: "DischargeLetter",
            title: "Discharge letter",
            mimeType: "application/pdf",
            storageRef: "inmem://documents/abc",
            contentHash: "00FF",
            size: 1234,
            source: DocumentReferenceSource.PdmsReporting,
            createdAtUtc: new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc),
            languageCode: "de");

        var fhir = DocumentReferenceMapper.ToFhir(doc);

        fhir.Status.ShouldBe(DocumentReferenceStatus.Current);
        fhir.Type.ShouldNotBeNull();
        fhir.Type!.Coding[0].Code.ShouldBe("DischargeLetter");
        fhir.Subject.ShouldNotBeNull();
        fhir.Subject!.Reference.ShouldBe("Patient/000000000000000000000000000000a1");
        fhir.Content[0].Attachment.ContentType.ShouldBe("application/pdf");
        fhir.Content[0].Attachment.Url.ShouldBe("inmem://documents/abc");
        fhir.Content[0].Attachment.Language.ShouldBe("de");
        fhir.Content[0].Attachment.Hash.ShouldBe([0x00, 0xFF]);
    }

    [Fact]
    public void To_Fhir_Maps_Entered_In_Error_Status()
    {
        var doc = MakeDocument();
        doc.EnterInError();

        var fhir = DocumentReferenceMapper.ToFhir(doc);

        fhir.Status.ShouldBe(DocumentReferenceStatus.EnteredInError);
    }

    private static DocumentReference MakeDocument() => new(
        id: Guid.CreateVersion7(),
        patientId: Guid.NewGuid(),
        kind: "BillingDocument",
        title: "Billing",
        mimeType: "application/pdf",
        storageRef: "inmem://documents/x",
        contentHash: "AA",
        size: 1,
        source: DocumentReferenceSource.PdmsReporting,
        createdAtUtc: DateTime.UtcNow);
}
