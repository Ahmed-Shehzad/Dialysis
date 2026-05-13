using Dialysis.SmartConnect.Attachments;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>
/// Maps <c>/smartconnect/v1/admin</c> routes for attachment metadata + byte retrieval. The byte endpoint
/// serves <c>Content-Type</c> from the stored MIME and is the SmartConnect equivalent of Mirth's
/// Administrator-side viewers (UG pp 120-123, listed N/A in scope-vs-mirth).
/// </summary>
public static class AttachmentEndpointExtensions
{
    public static IEndpointRouteBuilder MapSmartConnectAttachmentRoutes(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/smartconnect/v1/admin").WithTags("SmartConnect Admin");

        admin.MapGet(
                "/messages/{messageId:guid}/attachments",
                async (Guid messageId, IAttachmentStore store, CancellationToken ct) =>
                {
                    var list = await store.GetForMessageAsync(messageId, ct).ConfigureAwait(false);
                    var metadata = list.Select(a => new AttachmentMetadata(
                        a.Id, a.MessageId, a.FlowId, a.MimeType, a.SizeBytes, a.CreatedUtc)).ToList();
                    return Results.Ok(metadata);
                })
            .WithName("SmartConnect_ListMessageAttachments");

        admin.MapGet(
                "/attachments/{id:guid}/metadata",
                async (Guid id, IAttachmentStore store, CancellationToken ct) =>
                {
                    var att = await store.GetAsync(id, ct).ConfigureAwait(false);
                    return att is null
                        ? Results.NotFound()
                        : Results.Ok(new AttachmentMetadata(att.Id, att.MessageId, att.FlowId, att.MimeType, att.SizeBytes, att.CreatedUtc));
                })
            .WithName("SmartConnect_GetAttachmentMetadata");

        admin.MapGet(
                "/attachments/{id:guid}",
                async (Guid id, IAttachmentStore store, CancellationToken ct) =>
                {
                    var att = await store.GetAsync(id, ct).ConfigureAwait(false);
                    if (att is null) return Results.NotFound();
                    return Results.File(att.Data.ToArray(), att.MimeType, $"{att.Id}.bin");
                })
            .WithName("SmartConnect_DownloadAttachment");

        admin.MapDelete(
                "/attachments/{id:guid}",
                async (Guid id, IAttachmentStore store, CancellationToken ct) =>
                {
                    await store.DeleteAsync(id, ct).ConfigureAwait(false);
                    return Results.NoContent();
                })
            .WithName("SmartConnect_DeleteAttachment");

        admin.MapDelete(
                "/messages/{messageId:guid}/attachments",
                async (Guid messageId, IAttachmentStore store, CancellationToken ct) =>
                {
                    await store.DeleteForMessageAsync(messageId, ct).ConfigureAwait(false);
                    return Results.NoContent();
                })
            .WithName("SmartConnect_DeleteMessageAttachments");

        return endpoints;
    }

    private sealed record AttachmentMetadata(
        Guid Id,
        Guid MessageId,
        Guid FlowId,
        string MimeType,
        long SizeBytes,
        DateTimeOffset CreatedUtc);
}
