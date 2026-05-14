using System.Net;
using System.Net.Http.Json;
using System.Text;
using Dialysis.SmartConnect.CodeTemplates;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class CodeTemplateLibraryEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CodeTemplateLibraryEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Post_then_get_round_trips_library()
    {
        using var client = _factory.CreateClient();

        var libraryId = Guid.CreateVersion7();
        var payload = new
        {
            id = libraryId,
            name = "ApiTestLib",
            linkedFlowIds = Array.Empty<Guid>(),
            templates = new[]
            {
                new
                {
                    id = Guid.Empty,
                    libraryId = libraryId,
                    name = "addOne",
                    code = "function addOne(n){return n+1;}",
                    contexts = new[] { CodeTemplateContext.SourceTransformer },
                },
            },
        };

        var postResp = await client.PostAsJsonAsync("/smartconnect/v1/admin/code-template-libraries", payload);
        Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);

        var getResp = await client.GetAsync($"/smartconnect/v1/admin/code-template-libraries/{libraryId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var json = await getResp.Content.ReadAsStringAsync();
        Assert.Contains("addOne", json);
        Assert.Contains("ApiTestLib", json);
    }

    [Fact]
    public async Task Delete_returns_no_content_and_makes_get_404()
    {
        using var client = _factory.CreateClient();

        var libraryId = Guid.CreateVersion7();
        var payload = new
        {
            id = libraryId,
            name = "ToDelete",
            templates = Array.Empty<object>(),
        };
        await client.PostAsJsonAsync("/smartconnect/v1/admin/code-template-libraries", payload);

        var delResp = await client.DeleteAsync($"/smartconnect/v1/admin/code-template-libraries/{libraryId}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

        var getResp = await client.GetAsync($"/smartconnect/v1/admin/code-template-libraries/{libraryId}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    [Fact]
    public async Task Mirth_xml_import_creates_library()
    {
        using var client = _factory.CreateClient();
        const string xml = """
        <list>
          <codeTemplateLibrary>
            <id>77777777-7777-4777-8777-777777777777</id>
            <name>ImportedFromMirth</name>
            <codeTemplates>
              <codeTemplate>
                <id>88888888-8888-4888-8888-888888888888</id>
                <name>xmlImported</name>
                <properties>
                  <code>function xmlImported(){return 'OK';}</code>
                  <contextSet>
                    <contextType>SOURCE_TRANSFORMER</contextType>
                  </contextSet>
                </properties>
              </codeTemplate>
            </codeTemplates>
          </codeTemplateLibrary>
        </list>
        """;

        var importResp = await client.PostAsync(
            "/smartconnect/v1/admin/code-template-libraries/import-mirth-xml",
            new StringContent(xml, Encoding.UTF8, "application/xml"));
        Assert.Equal(HttpStatusCode.OK, importResp.StatusCode);

        var getResp = await client.GetAsync("/smartconnect/v1/admin/code-template-libraries/77777777-7777-4777-8777-777777777777");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var json = await getResp.Content.ReadAsStringAsync();
        Assert.Contains("ImportedFromMirth", json);
        Assert.Contains("xmlImported", json);
    }
}
