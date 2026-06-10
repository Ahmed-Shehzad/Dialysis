using System.Net;
using System.Net.Http.Json;
using System.Text;
using Dialysis.SmartConnect.CodeTemplates;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class CodeTemplateLibraryEndpointTests : IClassFixture<SmartConnectApiFactory>
{
    private readonly SmartConnectApiFactory _factory;

    public CodeTemplateLibraryEndpointTests(SmartConnectApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Post_Then_Get_Round_Trips_Library_Async()
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
                    libraryId,
                    name = "addOne",
                    code = "function addOne(n){return n+1;}",
                    contexts = new[] { CodeTemplateContext.SourceTransformer },
                },
            },
        };

        var postResp = await client.PostAsJsonAsync("/api/v1/admin/code-template-libraries", payload);
        Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);

        var getResp = await client.GetAsync($"/api/v1/admin/code-template-libraries/{libraryId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var json = await getResp.Content.ReadAsStringAsync();
        Assert.Contains("addOne", json);
        Assert.Contains("ApiTestLib", json);
    }

    [Fact]
    public async Task Delete_Returns_No_Content_And_Makes_Get_404_Async()
    {
        using var client = _factory.CreateClient();

        var libraryId = Guid.CreateVersion7();
        var payload = new
        {
            id = libraryId,
            name = "ToDelete",
            templates = Array.Empty<object>(),
        };
        await client.PostAsJsonAsync("/api/v1/admin/code-template-libraries", payload);

        var delResp = await client.DeleteAsync($"/api/v1/admin/code-template-libraries/{libraryId}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

        var getResp = await client.GetAsync($"/api/v1/admin/code-template-libraries/{libraryId}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    [Fact]
    public async Task Mirth_Xml_Import_Creates_Library_Async()
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

        using var importContent = new StringContent(xml, Encoding.UTF8, "application/Xml");
        var importResp = await client.PostAsync(
            "/api/v1/admin/code-template-libraries/import-mirth-Xml",
            importContent);
        Assert.Equal(HttpStatusCode.OK, importResp.StatusCode);

        var getResp = await client.GetAsync("/api/v1/admin/code-template-libraries/77777777-7777-4777-8777-777777777777");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var json = await getResp.Content.ReadAsStringAsync();
        Assert.Contains("ImportedFromMirth", json);
        Assert.Contains("xmlImported", json);
    }
}
