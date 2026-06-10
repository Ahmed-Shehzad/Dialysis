using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>Channel-attachment blob routes (<c>/flows/{id}/attachments/blob</c> upload / download).</summary>
public static partial class ManagementEndpointExtensions
{
    // Channel-attachment blob endpoints — let operators upload reference docs that exceed the
    // 1 MiB inline cap (PDFs, large profile bundles, sample DICOM, …). Bytes are persisted
    // through IAttachmentStore (same atomic metadata+bytes write the per-message attachment
    // store uses); MessageId is set to the flow id since channel attachments aren't tied to
    // a message. The returned ref is then dropped into the channel's Attachments array with
    // `storageRef: { kind: "blob", id: ... }`.
    internal static void MapChannelAttachmentBlobEndpoints(RouteGroupBuilder admin)
    {
        admin.MapPost(
                "/flows/{flowId:guid}/attachments/blob",
                async (
                    Guid flowId,
                    HttpRequest request,
                    IIntegrationFlowRepository repo,
                    IAttachmentStore attachments,
                    CancellationToken ct) =>
                {
                    if (await repo.GetByIdAsync(flowId, ct).ConfigureAwait(false) is null)
                    {
                        return Results.NotFound(new { error = $"Flow {flowId} not found." });
                    }
                    await using var buffer = new MemoryStream();
                    await request.Body.CopyToAsync(buffer, ct).ConfigureAwait(false);
                    if (buffer.Length == 0)
                    {
                        return Results.BadRequest(new { error = "Request body is empty." });
                    }
                    var data = buffer.ToArray();
                    var blobId = Guid.NewGuid();
                    var mime = request.ContentType ?? "application/octet-stream";
                    await attachments.AddAsync(new Attachment
                    {
                        Id = blobId,
                        FlowId = flowId,
                        MessageId = flowId, // synthetic: channel attachments don't belong to a message
                        MimeType = mime,
                        Data = data,
                        SizeBytes = data.Length,
                        CreatedUtc = DateTimeOffset.UtcNow,
                    }, ct).ConfigureAwait(false);
                    return Results.Ok(new
                    {
                        storageRef = new { kind = "blob", id = blobId, sizeBytes = data.Length },
                    });
                })
            .WithName("SmartConnect_UploadChannelAttachmentBlob");

        admin.MapGet(
                "/flows/{flowId:guid}/attachments/blob/{blobId:guid}",
                async (
                    Guid flowId,
                    Guid blobId,
                    string? mimeType,
                    IIntegrationFlowRepository repo,
                    IAttachmentStore attachments,
                    CancellationToken ct) =>
                {
                    var flow = await repo.GetByIdAsync(flowId, ct).ConfigureAwait(false);
                    if (flow is null)
                    {
                        return Results.NotFound(new { error = $"Flow {flowId} not found." });
                    }
                    // Authorise via the channel: the blob must be referenced by one of the flow's
                    // declared attachments. Anything else would let operators read every blob in
                    // the store by guessing ids.
                    var attRef = flow.Attachments.Find(a =>
                        a.StorageRef is not null && a.StorageRef.Id == blobId);
                    if (attRef is null)
                    {
                        return Results.NotFound(new { error = "Blob is not referenced by this channel." });
                    }
                    var att = await attachments.GetAsync(blobId, ct).ConfigureAwait(false);
                    if (att is null)
                    {
                        return Results.NotFound(new { error = $"Blob {blobId} not found in the store." });
                    }
                    var contentType = string.IsNullOrWhiteSpace(mimeType) ? attRef.MimeType : mimeType!;
                    return Results.File(att.Data.ToArray(), contentType, attRef.Name);
                })
            .WithName("SmartConnect_DownloadChannelAttachmentBlob");
    }
}
