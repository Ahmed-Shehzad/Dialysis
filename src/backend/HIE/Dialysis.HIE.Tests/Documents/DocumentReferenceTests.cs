using Dialysis.HIE.Documents.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.Documents;

public sealed class DocumentReferenceTests
{
    [Fact]
    public void New_Document_Is_Current_With_Zero_Signatures()
    {
        var doc = Make_Document();
        doc.Status.ShouldBe(DocumentReferenceStatus.Current);
        doc.Signatures.ShouldBeEmpty();
    }

    [Fact]
    public void Revise_Updates_Storage_Ref_Hash_And_Size()
    {
        var doc = Make_Document();
        doc.Revise("inmem://documents/v2", "BEEF", 4096, hasAcroForms: true, hasJavascript: false);

        doc.StorageRef.ShouldBe("inmem://documents/v2");
        doc.ContentHash.ShouldBe("BEEF");
        doc.Size.ShouldBe(4096);
        doc.HasAcroForms.ShouldBeTrue();
    }

    [Fact]
    public void Revise_On_Entered_In_Error_Document_Throws()
    {
        var doc = Make_Document();
        doc.EnterInError();

        Should.Throw<InvalidOperationException>(() => doc.Revise("ref", "hash", 1, false, false));
    }

    [Fact]
    public void Record_Signature_Stacks_Signatures()
    {
        var doc = Make_Document();
        doc.RecordSignature(new DocumentReferenceSignature(
            id: Guid.CreateVersion7(),
            documentReferenceId: doc.Id,
            signerKind: DocumentSignerKind.Platform,
            certThumbprint: "AA",
            signedAtUtc: DateTime.UtcNow));
        doc.RecordSignature(new DocumentReferenceSignature(
            id: Guid.CreateVersion7(),
            documentReferenceId: doc.Id,
            signerKind: DocumentSignerKind.User,
            certThumbprint: "BB",
            signedAtUtc: DateTime.UtcNow,
            signerUserId: "alice"));

        doc.Signatures.Count.ShouldBe(2);
        doc.Signatures[1].SignerKind.ShouldBe(DocumentSignerKind.User);
        doc.Signatures[1].SignerUserId.ShouldBe("alice");
    }

    [Fact]
    public void User_Signature_Without_User_Id_Throws()
    {
        Should.Throw<ArgumentException>(() => new DocumentReferenceSignature(
            id: Guid.CreateVersion7(),
            documentReferenceId: Guid.NewGuid(),
            signerKind: DocumentSignerKind.User,
            certThumbprint: "AA",
            signedAtUtc: DateTime.UtcNow));
    }

    [Fact]
    public void Enter_In_Error_Is_Idempotent()
    {
        var doc = Make_Document();
        doc.EnterInError();
        doc.EnterInError();
        doc.Status.ShouldBe(DocumentReferenceStatus.EnteredInError);
    }

    private static DocumentReference Make_Document() => new(
        id: Guid.CreateVersion7(),
        patientId: Guid.NewGuid(),
        kind: "DischargeLetter",
        title: "Discharge letter",
        mimeType: "application/pdf",
        storageRef: "inmem://documents/x",
        contentHash: "AA",
        size: 100,
        source: DocumentReferenceSource.PdmsReporting,
        createdAtUtc: DateTime.UtcNow);
}
