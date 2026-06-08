using System.Text;
using Dialysis.BuildingBlocks.Documents.Storage;
using Dialysis.HIE.Documents.Domain;
using Dialysis.HIE.Documents.Features.GetDocumentPreview;
using Dialysis.HIE.Documents.Ports;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.Documents;

public sealed class GetDocumentPreviewQueryTests
{
    [Fact]
    public async Task Pdf_Returns_Format_Without_Content_Async()
    {
        var (repo, blobs, doc) = await Setup_Async("application/pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var sut = new GetDocumentPreviewQueryHandler(repo, blobs);

        var preview = await sut.HandleAsync(new GetDocumentPreviewQuery(doc.Id), CancellationToken.None);

        preview.ShouldNotBeNull();
        preview!.Format.ShouldBe(DocumentPreviewFormat.Pdf);
        preview.Content.ShouldBeNull();
    }

    [Fact]
    public async Task Xml_Cda_Returns_Pretty_Printed_Content_Async()
    {
        const string cda = "<ClinicalDocument xmlns=\"urn:hl7-org:v3\"><id root=\"abc\"/></ClinicalDocument>";
        var (repo, blobs, doc) = await Setup_Async("application/hl7-cda+xml", Encoding.UTF8.GetBytes(cda));
        var sut = new GetDocumentPreviewQueryHandler(repo, blobs);

        var preview = await sut.HandleAsync(new GetDocumentPreviewQuery(doc.Id), CancellationToken.None);

        preview.ShouldNotBeNull();
        preview!.Format.ShouldBe(DocumentPreviewFormat.Xml);
        preview.RootElement.ShouldBe("ClinicalDocument");
        preview.DocumentTypeName.ShouldBe("HL7 CDA");
        preview.Content.ShouldNotBeNullOrEmpty();
        preview.Content!.ShouldContain("<ClinicalDocument");
        preview.Content.ShouldContain("\n");
    }

    [Fact]
    public async Task Office_Doc_Returns_Binary_Hint_Async()
    {
        var (repo, blobs, doc) = await Setup_Async(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            new byte[] { 0x50, 0x4B, 0x03, 0x04 });
        var sut = new GetDocumentPreviewQueryHandler(repo, blobs);

        var preview = await sut.HandleAsync(new GetDocumentPreviewQuery(doc.Id), CancellationToken.None);

        preview.ShouldNotBeNull();
        preview!.Format.ShouldBe(DocumentPreviewFormat.Binary);
        preview.Content.ShouldBeNull();
    }

    [Fact]
    public async Task Plain_Text_Returns_Verbatim_Content_Async()
    {
        var (repo, blobs, doc) = await Setup_Async("text/plain", Encoding.UTF8.GetBytes("hello world"));
        var sut = new GetDocumentPreviewQueryHandler(repo, blobs);

        var preview = await sut.HandleAsync(new GetDocumentPreviewQuery(doc.Id), CancellationToken.None);

        preview.ShouldNotBeNull();
        preview!.Format.ShouldBe(DocumentPreviewFormat.Text);
        preview.Content.ShouldBe("hello world");
    }

    private static async Task<(IDocumentReferenceRepository repo, IDocumentBlobStore blobs, DocumentReference doc)> Setup_Async(
        string mime, byte[] bytes)
    {
        var blobs = new InMemoryDocumentBlobStore();
        var documentId = Guid.CreateVersion7();
        var storageRef = await blobs.SaveAsync(documentId, mime, bytes, CancellationToken.None);
        var doc = new DocumentReference(
            id: documentId,
            patientId: Guid.NewGuid(),
            kind: "Preview",
            title: "Preview test",
            mimeType: mime,
            storageRef: storageRef,
            contentHash: "AA",
            size: bytes.LongLength,
            source: DocumentReferenceSource.AdminUpload,
            createdAtUtc: DateTime.UtcNow);
        var repo = new InMemoryDocumentReferenceRepository(doc);
        return (repo, blobs, doc);
    }

    private sealed class InMemoryDocumentReferenceRepository : IDocumentReferenceRepository
    {
        private readonly DocumentReference _doc;
        public InMemoryDocumentReferenceRepository(DocumentReference doc) => _doc = doc;
        public void Add(DocumentReference document) => throw new NotSupportedException();
        public Task<bool> TryAddIdempotentAsync(DocumentReference document, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<DocumentReference?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<DocumentReference?>(_doc.Id == id ? _doc : null);
        public Task<IReadOnlyList<DocumentReference>> ListAsync(
            Guid? patientId, string? kind, DocumentReferenceStatus? status,
            DocumentReferenceSource? source, int take, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<DocumentReference>> ListExpiredAsync(
            string kind, DateTime createdBefore, int take, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<DocumentReference>> ListForPatientAsync(
            Guid patientId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
