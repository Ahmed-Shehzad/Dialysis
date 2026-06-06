using Dialysis.HIE.Query;
using Dialysis.HIE.Query.Xca;
using Hl7.Fhir.Model;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Tests.Query;

public sealed class XcaDocumentClientTests
{
    [Fact]
    public async Task Query_Returns_Document_References_For_The_Partner_Patient_Async()
    {
        var query = new ScriptedQuery();
        query.On("DocumentReference?patient=ext-1",
        [
            new DocumentReference { Id = "doc-1", Status = DocumentReferenceStatus.Current },
        ]);
        var client = new XcaDocumentClient(query);

        var docs = await client.QueryDocumentsAsync(Guid.NewGuid(), "ext-1", "Treatment");

        docs.ShouldHaveSingleItem().Id.ShouldBe("doc-1");
    }

    [Fact]
    public async Task Retrieve_Uses_Inline_Data_When_Present_Async()
    {
        var inline = "ccd"u8.ToArray();
        var document = new DocumentReference
        {
            Content = [new DocumentReference.ContentComponent { Attachment = new Attachment { Data = inline } }],
        };
        var client = new XcaDocumentClient(new ScriptedQuery());

        var bytes = await client.RetrieveContentAsync(Guid.NewGuid(), document, "ext-1", "Treatment");

        bytes.ShouldBe(inline);
    }

    [Fact]
    public async Task Retrieve_Dereferences_The_Binary_Url_When_No_Inline_Data_Async()
    {
        var binaryBytes = "pdf-bytes"u8.ToArray();
        var query = new ScriptedQuery();
        query.On("Binary/bin-9", [new Binary { Id = "bin-9", Data = binaryBytes }]);
        var document = new DocumentReference
        {
            Content = [new DocumentReference.ContentComponent
            {
                Attachment = new Attachment { Url = "https://partner.example/fhir/Binary/bin-9" },
            }],
        };
        var client = new XcaDocumentClient(query);

        var bytes = await client.RetrieveContentAsync(Guid.NewGuid(), document, "ext-1", "Treatment");

        bytes.ShouldBe(binaryBytes);
        query.Requested.ShouldContain("Binary/bin-9");
    }

    private sealed class ScriptedQuery : IPartnerFhirQuery
    {
        private readonly Dictionary<string, IReadOnlyList<Resource>> _scripts = new(StringComparer.Ordinal);
        public List<string> Requested { get; } = [];

        public void On(string query, IReadOnlyList<Resource> resources) => _scripts[query] = resources;

        public Task<IReadOnlyList<Resource>> QueryAsync(Guid partnerId, string relativeQuery, string subject, string purposeOfUse, CancellationToken cancellationToken = default)
        {
            Requested.Add(relativeQuery);
            return Task.FromResult(_scripts.TryGetValue(relativeQuery, out var r) ? r : []);
        }
    }
}
