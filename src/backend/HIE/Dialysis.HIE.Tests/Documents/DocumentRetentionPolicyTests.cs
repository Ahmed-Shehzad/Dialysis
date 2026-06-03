using Dialysis.HIE.Documents.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.Documents;

public sealed class DocumentRetentionPolicyTests
{
    [Fact]
    public void New_Policy_Stamps_Created_And_Updated_Fields()
    {
        var now = new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
        var sut = new DocumentRetentionPolicy(
            id: Guid.CreateVersion7(),
            kind: "DischargeLetter",
            retentionDays: 3650,
            createdAtUtc: now,
            updatedBy: "dpo");

        sut.Kind.ShouldBe("DischargeLetter");
        sut.RetentionDays.ShouldBe(3650);
        sut.CreatedAtUtc.ShouldBe(now);
        sut.UpdatedAtUtc.ShouldBe(now);
        sut.UpdatedBy.ShouldBe("dpo");
    }

    [Fact]
    public void Non_Positive_Retention_Days_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new DocumentRetentionPolicy(
            id: Guid.CreateVersion7(), kind: "x", retentionDays: 0,
            createdAtUtc: DateTime.UtcNow, updatedBy: "dpo"));
    }

    [Fact]
    public void Revise_Updates_Days_And_Audit_Fields()
    {
        var policy = MakePolicy(retentionDays: 365);
        var laterNow = policy.CreatedAtUtc.AddDays(10);
        policy.Revise(retentionDays: 730, now: laterNow, updatedBy: "dpo-2");
        policy.RetentionDays.ShouldBe(730);
        policy.UpdatedAtUtc.ShouldBe(laterNow);
        policy.UpdatedBy.ShouldBe("dpo-2");
        policy.CreatedAtUtc.ShouldNotBe(laterNow);
    }

    [Fact]
    public void Document_Mark_Blob_Purged_Soft_Deletes_And_Tombstones_Storage_Ref()
    {
        var doc = new DocumentReference(
            id: Guid.CreateVersion7(),
            patientId: Guid.NewGuid(),
            kind: "DischargeLetter",
            title: "Discharge",
            mimeType: "application/pdf",
            storageRef: "inmem://documents/abc",
            contentHash: "AA",
            size: 1024,
            source: DocumentReferenceSource.PdmsReporting,
            createdAtUtc: DateTime.UtcNow);

        doc.MarkBlobPurged("retention");

        doc.Status.ShouldBe(DocumentReferenceStatus.EnteredInError);
        doc.StorageRef.ShouldBe("purged://retention");
        doc.Size.ShouldBe(0);
    }

    private static DocumentRetentionPolicy MakePolicy(int retentionDays) => new(
        id: Guid.CreateVersion7(),
        kind: "DischargeLetter",
        retentionDays: retentionDays,
        createdAtUtc: new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc),
        updatedBy: "dpo");
}
