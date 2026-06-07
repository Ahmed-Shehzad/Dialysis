using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Covers channel-level attachments stored out-of-row via <c>IAttachmentBlobStore</c>:
///  - Upload returns a <c>storageRef</c>.
///  - PUTting the flow with that ref in the Attachments array is accepted (cap waived).
///  - GET on the blob endpoint streams the bytes back.
///  - GETs against a blob id NOT referenced by the channel return 404 (no enumeration).
/// </summary>
public sealed class ChannelBlobAttachmentTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ChannelBlobAttachmentTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Blob_Upload_Then_Reference_On_Channel_Then_Download_Round_Trips_Async()
    {
        using var client = _factory.CreateClient();
        var flowId = await CreateFlow_Async(client, "blob-host", attachments: []);

        // 1. Upload a 2 MiB payload (above the inline cap on purpose).
        var bytes = new byte[2 * 1024 * 1024];
        new Random(42).NextBytes(bytes);
        using var uploadContent = new ByteArrayContent(bytes);
        uploadContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        var uploadResponse = await client.PostAsync(
            $"/api/v1/admin/flows/{flowId}/attachments/blob",
            uploadContent);
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        using var uploadDoc = JsonDocument.Parse(await uploadResponse.Content.ReadAsStringAsync());
        var storageRef = uploadDoc.RootElement.GetProperty("storageRef");
        var blobId = Guid.Parse(storageRef.GetProperty("id").GetString()!);
        Assert.Equal(bytes.Length, storageRef.GetProperty("sizeBytes").GetInt64());

        // 2. PUT the flow updated with this storageRef on an Attachments entry.
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/admin/flows/{flowId}",
            BuildFlowBody(flowId, "blob-host", attachments: new[]
            {
                new
                {
                    name = "huge-bundle.zip",
                    mimeType = "application/zip",
                    base64Bytes = "",
                    description = (string?)null,
                    storageRef = new { kind = "blob", id = blobId, sizeBytes = (long)bytes.Length },
                },
            }));
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        // 3. Download via the blob endpoint.
        var downloadResponse = await client.GetAsync(
            $"/api/v1/admin/flows/{flowId}/attachments/blob/{blobId}");
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(bytes, downloadedBytes);
    }

    [Fact]
    public async Task Blob_Download_For_Unreferenced_Id_Returns_404_Async()
    {
        using var client = _factory.CreateClient();
        var flowId = await CreateFlow_Async(client, "blob-unref-host", attachments: []);

        var randomBlobId = Guid.NewGuid();
        var response = await client.GetAsync(
            $"/api/v1/admin/flows/{flowId}/attachments/blob/{randomBlobId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Channel_Update_Rejects_StorageRef_Missing_Id_Async()
    {
        using var client = _factory.CreateClient();
        var flowId = await CreateFlow_Async(client, "blob-bad-ref", attachments: []);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/admin/flows/{flowId}",
            BuildFlowBody(flowId, "blob-bad-ref", attachments: new[]
            {
                new
                {
                    name = "bad.bin",
                    mimeType = "application/octet-stream",
                    base64Bytes = "",
                    description = (string?)null,
                    storageRef = new { kind = "blob", id = Guid.Empty, sizeBytes = (long?)null },
                },
            }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<Guid> CreateFlow_Async(HttpClient client, string name, object[] attachments)
    {
        var id = Guid.NewGuid();
        var body = BuildFlowBody(id, name, attachments);
        var response = await client.PostAsJsonAsync("/api/v1/admin/flows", body);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Create '{name}' returned {response.StatusCode}: {text}");
        return id;
    }

    private static object BuildFlowBody(Guid id, string name, object[] attachments) => new
    {
        id,
        name,
        runtimeState = 0,
        description = (string?)null,
        tags = Array.Empty<string>(),
        dataTypes = new[] { "HL7v2" },
        dependencies = Array.Empty<Guid>(),
        attachments,
        pipeline = new
        {
            routeFilters = new[] { new { kind = "allow-all" } },
            sourceTransformStages = Array.Empty<object>(),
            outboundRoutesSequential = false,
            outboundRoutes = new[]
            {
                new
                {
                    outboundAdapterKind = "pass-through",
                    outboundParametersJson = (string?)null,
                },
            },
            linkedLibraryIds = Array.Empty<string>(),
        },
    };
}
