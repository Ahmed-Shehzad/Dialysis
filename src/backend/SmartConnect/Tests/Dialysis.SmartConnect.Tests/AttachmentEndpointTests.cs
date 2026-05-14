using System.Net;
using System.Text;
using Dialysis.SmartConnect.Attachments;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class AttachmentEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory ;

    public AttachmentEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Get_Metadata_Then_Bytes_Round_Trips_Async()
    {
        Attachment added;
        using (var scope = _factory.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IAttachmentStore>();
            added = await store.AddAsync(new Attachment
            {
                Id = Guid.CreateVersion7(),
                MessageId = Guid.CreateVersion7(),
                FlowId = Guid.CreateVersion7(),
                MimeType = "text/plain",
                Data = Encoding.UTF8.GetBytes("api-bytes"),
                SizeBytes = 9,
                CreatedUtc = DateTimeOffset.UtcNow,
            }, CancellationToken.None);
        }

        using var client = _factory.CreateClient();

        var metaResp = await client.GetAsync($"/smartconnect/v1/admin/attachments/{added.Id}/metadata");
        Assert.Equal(HttpStatusCode.OK, metaResp.StatusCode);
        var metaJson = await metaResp.Content.ReadAsStringAsync();
        Assert.Contains("text/plain", metaJson);

        var bytesResp = await client.GetAsync($"/smartconnect/v1/admin/attachments/{added.Id}");
        Assert.Equal(HttpStatusCode.OK, bytesResp.StatusCode);
        Assert.Equal("text/plain", bytesResp.Content.Headers.ContentType?.MediaType);
        var bytes = await bytesResp.Content.ReadAsByteArrayAsync();
        Assert.Equal("api-bytes", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public async Task Delete_Removes_Attachment_Async()
    {
        Attachment added;
        using (var scope = _factory.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IAttachmentStore>();
            added = await store.AddAsync(new Attachment
            {
                Id = Guid.CreateVersion7(),
                MessageId = Guid.CreateVersion7(),
                FlowId = Guid.CreateVersion7(),
                MimeType = "text/plain",
                Data = Encoding.UTF8.GetBytes("kill"),
                SizeBytes = 4,
                CreatedUtc = DateTimeOffset.UtcNow,
            }, CancellationToken.None);
        }

        using var client = _factory.CreateClient();
        var del = await client.DeleteAsync($"/smartconnect/v1/admin/attachments/{added.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var meta = await client.GetAsync($"/smartconnect/v1/admin/attachments/{added.Id}/metadata");
        Assert.Equal(HttpStatusCode.NotFound, meta.StatusCode);
    }

    [Fact]
    public async Task List_For_Message_Returns_Metadata_Array_Async()
    {
        var messageId = Guid.CreateVersion7();
        using (var scope = _factory.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IAttachmentStore>();
            for (var i = 0; i < 2; i++)
            {
                await store.AddAsync(new Attachment
                {
                    Id = Guid.CreateVersion7(),
                    MessageId = messageId,
                    FlowId = Guid.CreateVersion7(),
                    MimeType = "text/plain",
                    Data = Encoding.UTF8.GetBytes($"r-{i}"),
                    SizeBytes = 3,
                    CreatedUtc = DateTimeOffset.UtcNow,
                }, CancellationToken.None);
            }
        }

        using var client = _factory.CreateClient();
        var resp = await client.GetAsync($"/smartconnect/v1/admin/messages/{messageId}/attachments");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("\"mimeType\"", body, StringComparison.OrdinalIgnoreCase);
    }
}
